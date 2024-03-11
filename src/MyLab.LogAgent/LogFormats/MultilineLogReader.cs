using System.Text;

namespace MyLab.LogAgent.LogFormats;

class MultilineLogReader : ILogReader
{
    private readonly bool _emptyLineDelimiter;
    readonly StringBuilder _sb = new ();
    private bool _lastLineIsSpace = false;

    public MultilineLogReader(bool emptyLineDelimiter)
    {
        _emptyLineDelimiter = emptyLineDelimiter;
    }

    public LogReaderResult ApplyNexLine(string logTextLine)
    {
        if (_emptyLineDelimiter)
        {
            if (string.IsNullOrWhiteSpace(logTextLine))
            {
                _lastLineIsSpace = true;
            }
            else if (_lastLineIsSpace && !char.IsWhiteSpace(logTextLine[0]))
            {
                return LogReaderResult.NewRecordDetected;
            }
            else
            {
                _lastLineIsSpace = false;
            }
        }
        else
        {
            if (_sb.Length != 0 && logTextLine.Length > 0 && !char.IsWhiteSpace(logTextLine[0]))
                return LogReaderResult.NewRecordDetected;
        }

        if (_sb.Length != 0)
            _sb.AppendLine();
        _sb.Append(logTextLine.TrimEnd());
        return LogReaderResult.Accepted;
    }

    public BuildString BuildString()
    {
        return new BuildString(_sb.ToString());
    }

    public void Cleanup()
    {
        _lastLineIsSpace = false;
        _sb.Clear();
    }
}