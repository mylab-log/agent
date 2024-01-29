using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools.LogMessageProc;

namespace MyLab.LogAgent.LogFormats
{
    interface ILogFormat
    {
        ILogReader? CreateReader();

        LogRecord? Parse(string logText, ILogMessageExtractor messageExtractor);
    }
}
