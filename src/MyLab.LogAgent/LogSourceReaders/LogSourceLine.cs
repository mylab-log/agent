using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.LogSourceReaders;

class LogSourceLine
{
    public string Text { get; private set; }
    public DateTime? Time { get; set; }
    public IEnumerable<LogProperty>? Properties { get; set; }
    
    public bool IsError { get; set; }

    public LogSourceLine(string text)
    {
        Text = text;
    }
}