using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools.LogMessageExtraction;

namespace MyLab.LogAgent.LogFormats
{
    class NetLogFormat : ILogFormat
    {
        public ILogReader CreateReader()
        {
            return new NetLogReader();
        }

        public LogRecord Parse(string logText, ILogMessageExtractor messageExtractor)
        {
            var props = new List<LogProperty>();

            NetFormatLogic.ExtractCategory(logText, out var category, out var leftText);

            props.Add(new LogProperty
            {
                Name = LogPropertyNames.Category,
                Value = category
            });

            return messageExtractor.ExtractAndCreateLogRecord(leftText, props);
        }
    }
}
