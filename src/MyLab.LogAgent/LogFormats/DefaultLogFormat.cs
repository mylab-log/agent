using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.LogFormats
{
    class DefaultLogFormat : ILogFormat
    {
        public ILogBuilder CreateBuilder()
        {
            return new MultilineLogBuilder();
        }

        public LogRecord? Parse(string logText)
        {
            return string.IsNullOrWhiteSpace(logText) 
                ? null
                : new LogRecord
                {
                    Time = DateTime.Now,
                    Message = logText
                };
        }
    }
}
