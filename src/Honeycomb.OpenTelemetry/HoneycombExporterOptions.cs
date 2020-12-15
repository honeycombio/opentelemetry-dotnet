using System.Diagnostics;
using OpenTelemetry;

namespace Honeycomb.OpenTelemetry
{
    public class HoneycombExporterOptions
    {
        public string TeamId { get; set; }
        public string DefaultDataSet { get; set; }

        /// <summary>
        /// Gets or sets the export processor type to be used with Honeycomb Exporter.
        /// </summary>
        public ExportProcessorType ExportProcessorType { get; set; } = ExportProcessorType.Batch;

        /// <summary>
        /// Gets or sets the BatchExportProcessor options. Ignored unless ExportProcessorType is BatchExporter.
        /// </summary>
        public BatchExportProcessorOptions<Activity> BatchExportProcessorOptions { get; set; } = new BatchExportProcessorOptions<Activity>();
    }
}
