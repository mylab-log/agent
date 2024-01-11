using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.LogFormats
{
    interface ILogFormat
    {
        ILogBuilder? CreateBuilder();

        LogRecord? Parse(string logText);
    }
}
