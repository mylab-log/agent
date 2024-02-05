using LogLevel = MyLab.LogAgent.Model.LogLevel;

namespace MyLab.LogAgent.LogFormats;

public interface ILogReader
{
    LogReaderResult ApplyNexLine(string logTextLine);

    public BuildString BuildString();

    void Cleanup();
}

public record BuildString(string Text, LogLevel ExtractedLogLevel = LogLevel.Undefined);