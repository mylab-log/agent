using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools.LogMessageProc;

namespace MyLab.LogAgent.LogFormats
{
    class DefaultLogFormat : ILogFormat
    {
        public ILogReader CreateReader()
        {
            return new MultilineLogReader();
        }

        public LogRecord? Parse(string logText, ILogMessageExtractor messageExtractor)
        {
            if (string.IsNullOrWhiteSpace(logText))
                return null;

            return messageExtractor.ExtractAndCreateLogRecord(logText);
        }
    }
}
