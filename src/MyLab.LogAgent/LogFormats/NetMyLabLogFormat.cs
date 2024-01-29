using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.LogFormats
{
    class NetMyLabLogFormat : ILogFormat
    {
        public ILogBuilder CreateBuilder()
        {
            return new NetLogBuilder();
        }

        public LogRecord? Parse(string logText)
        {
            throw new NotImplementedException();
        }
    }
}
