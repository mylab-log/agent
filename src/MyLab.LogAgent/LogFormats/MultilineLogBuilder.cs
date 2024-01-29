using System.Text;

namespace MyLab.LogAgent.LogFormats;

class MultilineLogBuilder : ILogBuilder
{
    readonly StringBuilder _sb = new ();

    public LogReaderResult ApplyNexLine(string logTextLine)
    {
        if (_sb.Length != 0 && logTextLine.Length > 0 && !char.IsWhiteSpace(logTextLine[0]))
            return LogReaderResult.NewRecordDetected;

        if (_sb.Length != 0)
            _sb.AppendLine();
        _sb.Append(logTextLine.TrimEnd());
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