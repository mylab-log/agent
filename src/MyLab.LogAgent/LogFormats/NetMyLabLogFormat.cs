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
            throw new NotImplementedException();
        }
    }
}
