namespace MyLab.LogAgent.LogFormats;

class SingleLineLogBuilder : ILogBuilder
{
    private string? _text;

    public LogReaderResult ApplyNexLine(string logTextLine)
    {
        _text = logTextLine;
        return LogReaderResult.CompleteRecord;
    }

    public string? BuildString()
    {
        return _text;
    }

    public void Cleanup()
    {
        _text = null;
    }
}