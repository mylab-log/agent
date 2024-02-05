using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools.LogMessageExtraction;

namespace MyLab.LogAgent.LogFormats
{
    public interface ILogFormat
    {
        ILogReader? CreateReader();

        LogRecord? Parse(string logText, ILogMessageExtractor messageExtractor);
    }
}
