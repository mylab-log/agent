using MyLab.Log;

namespace MyLab.LogAgent.Model
{
    public class LogRecord
    {
        public DateTime Time { get; set; } = DateTime.Now;
        public required string Message { get; init; }
        public IEnumerable<KeyValuePair<string, string>>? Properties { get; set; }
    }
}
