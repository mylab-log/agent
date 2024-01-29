namespace MyLab.LogAgent.LogFormats;

class SingleLineLogReader : ILogReader
{
    private string _text;

    public LogReaderResult ApplyNexLine(string logTextLine)
    {
        _text = logTextLine;
        return LogReaderResult.CompleteRecord;
    }

    public BuildString BuildString()
    {
        return new BuildString(_text);
    }

    public void Cleanup()
    {
        _text = string.Empty;
    }
}