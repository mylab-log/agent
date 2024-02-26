using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools.LogMessageExtraction;

namespace MyLab.LogAgent.LogFormats
{
    class DefaultLogFormat : ILogFormat
    {
        public ILogReader CreateReader()
        {
            return new MultilineLogReader(false);
        }

        public LogRecord? Parse(string logText, ILogMessageExtractor messageExtractor)
        {
            if (string.IsNullOrWhiteSpace(logText))
                return null;

            return messageExtractor.ExtractAndCreateLogRecord(logText);
        }
    }
}
