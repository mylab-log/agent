using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.LogFormats
{
    class NetMyLabLogFormat : ILogFormat
    {
        public ILogReader CreateBuilder()
        {
            return new NetLogReader();
        }

        public LogRecord? Parse(string logText)
        {
            throw new NotImplementedException();
        }
    }
}
