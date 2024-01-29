using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.LogFormats
{
    interface ILogFormat
    {
        ILogReader? CreateReader();

        LogRecord? Parse(string logText);
    }
}
