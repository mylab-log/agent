using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.Tools.LogMessageExtraction;

public interface ILogMessageExtractor
{
    LogMessage Extract(string originMessage);
}

static class LogMessageExtractorExtensions
{
    public static LogRecord ExtractAndCreateLogRecord(this ILogMessageExtractor extractor, string originMessage, LogProperties? properties = null)
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

        properties ??= new();
        properties.Add(LogPropertyNames.OriginMessage,  msg.Full);

        return new LogRecord
        {
            Message = msg.Short,
            Properties = properties
        };
    }
} 