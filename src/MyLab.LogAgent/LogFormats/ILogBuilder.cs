using LogLevel = MyLab.LogAgent.Model.LogLevel;

namespace MyLab.LogAgent.LogFormats;

interface ILogBuilder
{
    LogReaderResult ApplyNexLine(string? logTextLine);

    BuiltString BuildString();

    void Cleanup();
}

record BuiltString(string? Text, LogLevel ExtractedLogLevel = LogLevel.Undefined);