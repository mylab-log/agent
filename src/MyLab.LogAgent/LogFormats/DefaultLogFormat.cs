using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.LogFormats
{
    class DefaultLogFormat : ILogFormat
    {
        public ILogReader CreateReader()
        {
            return new MultilineLogReader();
        }

        public LogRecord? Parse(string logText)
        {
            return string.IsNullOrWhiteSpace(logText) 
                ? null
                : new LogRecord
                {
                    Message = logText
                };
        }
    }
}
