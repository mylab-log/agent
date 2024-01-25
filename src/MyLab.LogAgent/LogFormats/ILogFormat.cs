using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.LogFormats
{
    interface ILogFormat
    {
        string Name { get; }

        ILogBuilder? CreateBuilder();

        LogRecord? Parse(string logText);
    }
}
