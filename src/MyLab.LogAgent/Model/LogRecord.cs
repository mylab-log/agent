using MyLab.Log;

namespace MyLab.LogAgent.Model
{
    public class LogRecord
    {
        public DateTime Time { get; set; } = default;
        public required string Message { get; init; }
        public LogLevel Level { get; set; }
        public string? Format { get; set; }
        public string? Container { get; set; }
        public List<LogProperty>? Properties { get; set; }
    }

    public enum LogLevel
    {
        Undefined,
        Info,
        Warning,
        Error
    }
}
