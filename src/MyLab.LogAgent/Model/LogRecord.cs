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
        public LogProperties? Properties { get; set; }

        public int OriginLinesCount { get; set; }
        public int OriginBytesCount { get; set; }
        public bool HasParsingError { get; set; }

        public void SetParsingErrorState(string failureReasonId, Exception? exception = null)
        {
            Properties ??= new LogProperties();

            Properties.Add(LogPropertyNames.ParsingFailedFlag, "true");
            Properties.Add(LogPropertyNames.ParsingFailureReason, failureReasonId);

            if (exception != null)
            {
                Properties.Add(
                    LogPropertyNames.Exception,
                    ExceptionDto.Create(exception).ToYaml() ?? "[no-error-yaml]"
                    );
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
