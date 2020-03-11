using System;
using OpenTelemetry.Trace.Configuration;
using OpenTelemetry.Trace.Export;

namespace Honeycomb.OpenTelemetry
{
    public static class TracerBuilderExtensions
    {
        public static TracerBuilder UseHoneycomb(this TracerBuilder builder, IServiceProvider serviceProvider)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddProcessorPipeline(b => b
                .SetExporter(serviceProvider.GetService(typeof(HoneycombExporter)) as HoneycombExporter)
                .SetExportingProcessor(e => new BatchingSpanProcessor(e)));
        }
    }
}
