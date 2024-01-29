using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools.LogMessageProc;

namespace MyLab.LogAgent.LogFormats
{
    class MyLabLogFormat : ILogFormat
    {
        public ILogReader CreateReader()
        {
            return new MyLabLogReader();
        }

        public LogRecord Parse(string logText, ILogMessageExtractor messageExtractor)
        {
            return MyLabFormatLogic.Parse(logText, messageExtractor);
        }
    }
}
