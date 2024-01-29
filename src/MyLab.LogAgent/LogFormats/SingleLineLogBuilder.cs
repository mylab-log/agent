namespace MyLab.LogAgent.LogFormats;

class SingleLineLogBuilder : ILogBuilder
{
    private string _text;

    public LogReaderResult ApplyNexLine(string logTextLine)
    {
        _text = logTextLine;
        return LogReaderResult.CompleteRecord;
    }

    public BuiltString BuildString()
    {
        return new BuiltString(_text);
    }

    public void Cleanup()
    {
        _text = string.Empty;
    }
}