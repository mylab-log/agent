namespace MyLab.LogAgent.LogFormats;

interface ILogBuilder
{
    LogReaderResult ApplyNexLine(string? logTextLine);

    string? BuildString();

    void Cleanup();
}