using System.Text;
using LogLevel = MyLab.LogAgent.Model.LogLevel;

namespace MyLab.LogAgent.LogFormats;

class NetLogReader : ILogReader
{
    private readonly StringBuilder _sb = new();
    private LogLevel _logLevel;

    public LogReaderResult ApplyNexLine(string logTextLine)
    {
        if (_sb.Length != 0 && logTextLine.StartsWith('\u001b'))
            return LogReaderResult.NewRecordDetected;

        if (logTextLine.StartsWith('\u001b'))
        {
            switch (new string(logTextLine.ToCharArray(), 10, 4))
            {
                case "info":
                    _logLevel = LogLevel.Info;
                    break;
                case "fail":
                case "crit":
                    _logLevel = LogLevel.Error;
                    break;
                case "dbug":
                    _logLevel = LogLevel.Debug;
                    break;
                case "warn":
                    _logLevel = LogLevel.Warning;
                    break;
            }

            _sb.AppendLine(logTextLine.Substring(31).TrimEnd());
        }
        else
        {
            _sb.AppendLine(logTextLine.TrimEnd());
        }

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