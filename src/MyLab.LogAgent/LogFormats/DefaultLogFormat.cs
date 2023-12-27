using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.LogFormats
{
    class DefaultLogFormat : ILogFormat
    {
        public ILogBuilder CreateBuilder()
        {
            return new DefaultMultilineLogBuilder();
        }

        public LogRecord? Deserialize(string logText)
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
