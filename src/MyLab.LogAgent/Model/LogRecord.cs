using MyLab.Log;
using MyLab.LogAgent.Services;
using MyLab.LogAgent.Tools;

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

        public int OriginLinesCount { get; set; }
        public int OriginBytesCount { get; set; }
        public bool HasParsingError { get; set; }

        public void SetParsingErrorState(string failureReasonId, Exception? exception = null)
        {
            Properties ??= new List<LogProperty>();

            Properties.Add(new LogProperty
            {
                Name = LogPropertyNames.ParsingFailedFlag,
                Value = "true"
            });
            Properties.Add(new LogProperty
            {
                Name = LogPropertyNames.ParsingFailureReason,
                Value = failureReasonId
            });

            if (exception != null)
            {
                Properties.Add(new LogProperty
                {
                    Name = LogPropertyNames.Exception,
                    Value = ExceptionDto.Create(exception).ToYaml() ?? "[no-error-yaml]"
                });
            }

            HasParsingError = true;
        }
    }

    public enum LogLevel
    {
        Undefined,
        Debug,
        Info,
        Warning,
        Error
    }
}
