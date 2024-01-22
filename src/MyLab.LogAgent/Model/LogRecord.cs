using MyLab.Log;

namespace MyLab.LogAgent.Model
{
    public class LogRecord
    {
        public DateTime Time { get; set; } = default;
        public required string Message { get; init; }
        public List<LogProperty>? Properties { get; set; }
    }
}
