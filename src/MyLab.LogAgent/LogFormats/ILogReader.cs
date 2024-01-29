using LogLevel = MyLab.LogAgent.Model.LogLevel;

namespace MyLab.LogAgent.LogFormats;

interface ILogReader
{
    LogReaderResult ApplyNexLine(string logTextLine);

    BuildString BuildString();

    void Cleanup();
}

record BuildString(string Text, LogLevel ExtractedLogLevel = LogLevel.Undefined);