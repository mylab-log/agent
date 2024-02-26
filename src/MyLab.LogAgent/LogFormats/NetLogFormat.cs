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
            return NetFormatLogic.Parse(logText, messageExtractor);
        }
    }
}
