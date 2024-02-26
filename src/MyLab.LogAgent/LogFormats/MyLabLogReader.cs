using System.Text;
using MyLab.Log;

namespace MyLab.LogAgent.LogFormats;

class MyLabLogReader : ILogReader
{
    private readonly StringBuilder _sb = new();

    public static Func<string, bool> NewRecordPredicate = l => l.StartsWith(nameof(LogEntity.Message) + ": ");

    public LogReaderResult ApplyNexLine(string logTextLine)
    {
        if (_sb.Length != 0 && NewRecordPredicate(logTextLine))
            return LogReaderResult.NewRecordDetected;

        _sb.AppendLine(logTextLine.TrimEnd());

        return LogReaderResult.Accepted;
    }

    public BuildString BuildString()
    {
        return new BuildString(_sb.ToString());
    }

    public void Cleanup()
    {
        _sb.Clear();
    }
}