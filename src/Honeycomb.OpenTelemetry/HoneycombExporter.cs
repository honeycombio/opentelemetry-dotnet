using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Honeycomb.Models;
using OpenTelemetry.Trace.Export;
using Microsoft.Extensions.Options;
using System;

namespace Honeycomb.OpenTelemetry
{
    public class HoneycombExporter : SpanExporter
    {
        private readonly IHoneycombService _honeycombService;
        private readonly IOptions<HoneycombApiSettings> _settings;

        public HoneycombExporter(IHoneycombService honeycombService, IOptions<HoneycombApiSettings> settings)
        {
            _honeycombService = honeycombService;
            _settings = settings;
        }

        public async override Task<ExportResult> ExportAsync(IEnumerable<SpanData> batch, CancellationToken cancellationToken)
        {
            var honeycombEvents = new List<HoneycombEvent>();

            foreach (var span in batch)
            {
                if (span.Attributes
                        .Any(a => a.Key == "http.url" && 
                                 (a.Value is string urlStr && 
                                  urlStr.StartsWith("https://api.honeycomb.io/"))))
                    continue;
                
                var events = GenerateEvent(span);
                honeycombEvents.AddRange(events);
            }
            await _honeycombService.SendBatchAsync(honeycombEvents);

            return ExportResult.Success;
        }

        private IEnumerable<HoneycombEvent> GenerateEvent(SpanData span)
        {
            var list = new List<HoneycombEvent>();

            var ev = new HoneycombEvent {
                EventTime = span.StartTimestamp.UtcDateTime,
                DataSetName = _settings.Value.DefaultDataSet
            };
            var baseAttributes = new Dictionary<string, object> {
                {"trace.trace_id", span.Context.TraceId.ToString()},
                {"service_name", span.Name}
            };
            if (span.ParentSpanId.ToString() != "0000000000000000")
                ev.Data.Add("trace.parent_id", span.ParentSpanId.ToString());

            ev.Data.AddRange(baseAttributes);
            ev.Data.Add("trace.span_id", span.Context.SpanId.ToString());
            ev.Data.Add("duration_ms", (span.EndTimestamp - span.StartTimestamp).TotalMilliseconds);

            foreach (var label in span.Attributes)
            {
                ev.Data.Add(label.Key, label.Value.ToString());
            }

            foreach (var attr in span.LibraryResource.Attributes)
            {
                ev.Data.Add(attr.Key, attr.Value);
            }

            foreach (var message in span.Events)
            {
                var messageEvent = new HoneycombEvent {
                    EventTime = message.Timestamp.UtcDateTime,
                    DataSetName = _settings.Value.DefaultDataSet,
                    Data = message.Attributes.ToDictionary(a => a.Key, a => a.Value)
                };
                messageEvent.Data.Add("meta.annotation_type", "span_event");
                messageEvent.Data.Add("trace.parent_id", span.Context.SpanId.ToString());
                messageEvent.Data.Add("name", message.Name);
                messageEvent.Data.AddRange(baseAttributes);
                list.Add(messageEvent);
            }

            foreach (var link in span.Links)
            {
                var linkEvent = new HoneycombEvent {
                    EventTime = span.StartTimestamp.UtcDateTime,
                    DataSetName = _settings.Value.DefaultDataSet,
                    Data = link.Attributes.ToDictionary(a => a.Key, a => a.Value)
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

        public override Task ShutdownAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
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
