using System;
using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Honeycomb.OpenTelemetry
{
    public static class TracerBuilderExtensions
    {
        /// <summary>
        /// Adds honeycomb exporter to the TracerProvider.
        /// </summary>
        /// <param name="builder"><see cref="TracerProviderBuilder"/> builder to use.</param>
        /// <param name="honeycombService"></param>
        /// <param name="configure">Exporter configuration options.</param>
        /// <returns>The instance of <see cref="TracerProviderBuilder"/> to chain the calls.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The objects should not be disposed.")]
        public static TracerProviderBuilder AddHoneycombExporter(
            this TracerProviderBuilder builder,
            IHoneycombService honeycombService,
            Action<HoneycombExporterOptions> configure = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (honeycombService == null) throw new ArgumentNullException(nameof(honeycombService));

            var exporterOptions = new HoneycombExporterOptions();
            configure?.Invoke(exporterOptions);

            var HoneycombExporter = new HoneycombExporter(exporterOptions, honeycombService);

            if (exporterOptions.ExportProcessorType == ExportProcessorType.Simple)
            {
                return builder.AddProcessor(new SimpleExportProcessor<Activity>(HoneycombExporter));
            }
            else
            {
                return builder.AddProcessor(new BatchExportProcessor<Activity>(
                    HoneycombExporter,
                    exporterOptions.BatchExportProcessorOptions.MaxQueueSize,
                    exporterOptions.BatchExportProcessorOptions.ScheduledDelayMilliseconds,
                    exporterOptions.BatchExportProcessorOptions.ExporterTimeoutMilliseconds,
                    exporterOptions.BatchExportProcessorOptions.MaxExportBatchSize));
            }
        }
    }
}
