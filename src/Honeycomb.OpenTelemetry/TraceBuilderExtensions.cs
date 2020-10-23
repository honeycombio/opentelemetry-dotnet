using System;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Honeycomb.OpenTelemetry
{
    public static class TracerBuilderExtensions
    {
        public static TracerProviderBuilder UseHoneycomb(this TracerProviderBuilder builder, IServiceProvider serviceProvider)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddProcessor(
                new BatchExportProcessor<Activity>(serviceProvider.GetService(typeof(HoneycombExporter)) as HoneycombExporter)
            );
        }
    }
}
