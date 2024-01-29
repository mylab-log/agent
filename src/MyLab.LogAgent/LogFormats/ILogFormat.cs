using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.LogFormats
{
    interface ILogFormat
    {
        ILogReader? CreateBuilder();

        LogRecord? Parse(string logText);
    }
}
