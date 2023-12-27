namespace MyLab.LogAgent.Model
{
    public class LogRecord
    {
        public required string Message { get; init; }
        public IDictionary<string, string>? Properties { get; set; }
    }
}
