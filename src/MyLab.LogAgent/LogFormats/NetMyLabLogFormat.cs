using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools.LogMessageProc;

namespace MyLab.LogAgent.LogFormats
{
    class NetMyLabLogFormat : ILogFormat
    {
        public ILogReader CreateReader()
        {
            return new NetLogReader();
        }

        public LogRecord? Parse(string logText, ILogMessageExtractor messageExtractor)
        {
            NetFormatLogic.ExtractCategory(logText, out var category, out var leftText);

            var catProp = new LogProperty
            {
                Name = LogPropertyNames.Category,
                Value = category
            };

            return MyLabFormatLogic.Parse(leftText, messageExtractor, Enumerable.Repeat(catProp, 1));
        }
    }
}
