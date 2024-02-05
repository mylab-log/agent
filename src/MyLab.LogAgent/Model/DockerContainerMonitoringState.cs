using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.LogSourceReaders;

namespace MyLab.LogAgent.Model;

public class DockerContainerMonitoringState
{
    public required DockerContainerInfo Info { get; init; }
    public long Shift { get; set; }
    public List<LogSourceLine> LineBuff { get; } = new();
    public string? FormatName { get; set; }
    public ILogFormat? Format { get; set; }
    public bool UnsupportedFormatDetected { get; set; }
    public DockerContainerLastIterationParameters LastIteration { get; } = new();
}