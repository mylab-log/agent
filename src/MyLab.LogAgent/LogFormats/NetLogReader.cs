using System.Text;
using LogLevel = MyLab.LogAgent.Model.LogLevel;

namespace MyLab.LogAgent.LogFormats;

class NetLogReader : ILogReader
{
    private readonly StringBuilder _sb = new();
    private LogLevel _logLevel;

    public static Func<string, bool> NewRecordPredicate = l => l.StartsWith('\u001b');

    public LogReaderResult ApplyNexLine(string logTextLine)
    {
        if (_sb.Length != 0 && NewRecordPredicate(logTextLine))
            return LogReaderResult.NewRecordDetected;

        _sb.AppendLine(logTextLine.TrimEnd());

        return LogReaderResult.Accepted;
    }

    public BuildString BuildString()
    {
        return new BuildString(_sb.ToString(), _logLevel);
    }

    public void Cleanup()
    {
        _logLevel = LogLevel.Undefined;
        _sb.Clear();
    }
}