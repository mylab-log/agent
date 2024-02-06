using MyLab.LogAgent.Model;
using Prometheus;

namespace MyLab.LogAgent.Services
{
    public interface IMetricsOperator
    {
        void UpdateReadingMetrics(LogRecord logRecord);
        void UpdateContainerMetrics(DockerContainerSyncReport syncReport);
    }

    class MetricsOperator : IMetricsOperator
    {
        private const string UndefinedLabel = "[undefined]";

        static readonly Counter ReadRecordCounter = Metrics.CreateCounter("loga_read_records_total", "Count of read log records",
            labelNames:
            [
                "container",
                "format",
                "level"
            ]);

        static readonly Counter ReadLinesCounter = Metrics.CreateCounter("loga_read_lines_total", "Count of read log lines",
            labelNames:
            [
                "container",
                "format",
                "level"
            ]);

        static readonly Counter ReadBytesCounter = Metrics.CreateCounter("loga_read_bytes_total", "Count of read log bytes",
            labelNames:
            [
                "container",
                "format",
                "level"
            ]);

        static readonly Counter ParsingErrorCounter = Metrics.CreateCounter("loga_parsing_error_total", "Count of parsing errors",
            labelNames:
            [
                "container",
                "format",
                "level"
            ]);

        static readonly Gauge ContainerNumber = Metrics.CreateGauge("loga_container_number", "Number of target containers",
            labelNames:
            [
                "container",
                "format",
                "enabled"
            ]);

        public void UpdateReadingMetrics(LogRecord logRecord)
        {
            string[] labels = new[]
            {
                logRecord.Container ?? UndefinedLabel,
                logRecord.Format ?? UndefinedLabel,
                logRecord.Level.ToString()
            };

            ReadRecordCounter.WithLabels(labels).Inc();
            ReadLinesCounter.WithLabels(labels).Inc(logRecord.OriginLinesCount);
            ReadBytesCounter.WithLabels(labels).Inc(logRecord.OriginBytesCount);

            if (logRecord.HasParsingError)
                ParsingErrorCounter.WithLabels(labels).Inc();
        }

        public void UpdateContainerMetrics(DockerContainerSyncReport syncReport)
        {
            foreach (var removed in syncReport.Removed)
            {
                ContainerNumber.RemoveLabelled(
                    removed.Info.Name,
                    removed.Info.LogFormat ?? UndefinedLabel,
                    removed.Info.Enabled.ToString()
                    );
            }

            foreach (var added in syncReport.Added)
            {
                ContainerNumber.WithLabels(
                    added.Info.Name,
                    added.Info.LogFormat ?? UndefinedLabel,
                    added.Info.Enabled.ToString()
                ).Set(1);
            }
        }
    }
}
