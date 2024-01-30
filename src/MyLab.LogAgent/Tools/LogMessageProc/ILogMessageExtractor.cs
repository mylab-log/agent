using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.Tools.LogMessageProc;

interface ILogMessageExtractor
{
    LogMessage Extract(string originMessage);
}

static class LogMessageExtractorExtensions
{
    public static LogRecord ExtractAndCreateLogRecord(this ILogMessageExtractor extractor, string originMessage, List<LogProperty>? properties = null)
    {
        var msg = extractor.Extract(originMessage);

        if (!msg.Shorted)
        {
            return new LogRecord
            {
                Message = msg.Full,
                Properties = properties
            };
        }

        properties ??= [];
        properties.Add(new()
        {
            Name = LogPropertyNames.OriginMessage,
            Value = msg.Full
        });

        return new LogRecord
        {
            Message = msg.Short,
            Properties = properties
        };
    }
} 