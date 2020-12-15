using System.Collections.Generic;
using System.Linq;
using Honeycomb.Models;
using Microsoft.Extensions.Options;
using System;
using OpenTelemetry;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Honeycomb.OpenTelemetry
{
    public class HoneycombExporter : BaseExporter<Activity>
    {
        private readonly HoneycombExporterOptions _options;
        private readonly IHoneycombService _honeycombService;

        internal HoneycombExporter(HoneycombExporterOptions options, IHoneycombService honeycombService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _honeycombService = honeycombService ?? throw new ArgumentNullException(nameof(honeycombService));
        }

        public override ExportResult Export(in Batch<Activity> batch)
        {
            using (var scope = SuppressInstrumentationScope.Begin())
            {
                try
                {
                    var honeycombEvents = new List<HoneycombEvent>();
                    foreach (var activity in batch)
                    {
                        // TODO: skip activities where url starts with https://api.honeycomb.com

                        var events = GenerateEvent(activity);
                        honeycombEvents.AddRange(events);
                    }

                    _honeycombService.SendBatchAsync(honeycombEvents).ConfigureAwait(false).GetAwaiter().GetResult();
                    return ExportResult.Success;
                }
                catch (Exception)
                {
                    return ExportResult.Failure;
                }
            }
        }

        private IEnumerable<HoneycombEvent> GenerateEvent(Activity activity)
        {
            var list = new List<HoneycombEvent>();

            var ev = new HoneycombEvent {
                EventTime = activity.StartTimeUtc,
                DataSetName = _options.DefaultDataSet
            };
            var baseAttributes = new Dictionary<string, object> {
                {"trace.trace_id", activity.Context.TraceId.ToString()},
                {"service_name", activity.DisplayName}
            };
            if (activity.ParentSpanId.ToString() != "0000000000000000")
                ev.Data.Add("trace.parent_id", activity.ParentSpanId.ToString());

            ev.Data.AddRange(baseAttributes);
            ev.Data.Add("trace.span_id", activity.Context.SpanId.ToString());
            ev.Data.Add("duration_ms", activity.Duration.Milliseconds);

            foreach (var label in activity.Tags)
            {
                ev.Data.Add(label.Key, label.Value.ToString());
            }

            var resource = this.ParentProvider.GetResource();
            foreach (var attribute in resource.Attributes)
            {
                // map service.name to service_name
                if (attribute.Key == "service.name")
                {
                    ev.Data["service_name"] = attribute.Value;
                }
                else
                {
                    ev.Data.Add(attribute.Key, attribute.Value);
                }
            }

            foreach (var message in activity.Events)
            {
                var messageEvent = new HoneycombEvent {
                    EventTime = message.Timestamp.UtcDateTime,
                    DataSetName = _options.DefaultDataSet,
                    Data = message.Tags.ToDictionary(a => a.Key, a => a.Value)
                };
                messageEvent.Data.Add("meta.annotation_type", "span_event");
                messageEvent.Data.Add("trace.parent_id", activity.Context.SpanId.ToString());
                messageEvent.Data.Add("name", message.Name);
                messageEvent.Data.AddRange(baseAttributes);
                list.Add(messageEvent);
            }

            foreach (var link in activity.Links)
            {
                var linkEvent = new HoneycombEvent {
                    EventTime = activity.StartTimeUtc,
                    DataSetName = _options.DefaultDataSet,
                    Data = link.Tags.ToDictionary(a => a.Key, a => a.Value)
                };
                linkEvent.Data.Add("meta.annotation_type", "link");
                linkEvent.Data.Add("trace.link.span_id", link.Context.SpanId.ToString());
                linkEvent.Data.Add("trace.link.trace_id", link.Context.TraceId.ToString());
                linkEvent.Data.AddRange(baseAttributes);
                list.Add(linkEvent);
            }

            list.Add(ev);
            return list;
        }
    }

    public static class DictionaryExtensions
    {
        public static void AddRange<T, T1>(this Dictionary<T, T1> dest, Dictionary<T, T1> source)
        {
            foreach (var kvp in source)
                dest.Add(kvp.Key, kvp.Value);
        }
    }
}
