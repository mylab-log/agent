namespace MyLab.LogAgent.Tools.LogMessageProc;

interface ILogMessageExtractor
{
    LogMessage Extract(string originMessage);
}