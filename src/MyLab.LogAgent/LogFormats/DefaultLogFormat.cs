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

            var msg = messageExtractor.Extract(logText);

            if (!msg.Shorted)
            {
                return new LogRecord
                {
                    Message = msg.Full
                };
            }

            return new LogRecord
            {
                Message = msg.Short,
                Properties = new List<LogProperty>()
                {
                    new()
                    {
                        Name = LogPropertyNames.OriginMessage,
                        Value = msg.Full
                    }
                }
            };
        }
    }
}
