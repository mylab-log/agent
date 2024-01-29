using System.Text;
using MyLab.Log;

namespace MyLab.LogAgent.LogFormats;

class MyLabLogBuilder : ILogBuilder
{
    private readonly StringBuilder _sb = new();

    public LogReaderResult ApplyNexLine(string logTextLine)
    {
        if (_sb.Length != 0 && logTextLine.StartsWith(nameof(LogEntity.Message) + ": "))
            return LogReaderResult.NewRecordDetected;

        _sb.AppendLine(logTextLine.TrimEnd());

        return LogReaderResult.Accepted;
    }

    public BuiltString BuildString()
    {
        return new BuiltString(_sb.ToString());
    }

    public void Cleanup()
    {
        _sb.Clear();
    }
}