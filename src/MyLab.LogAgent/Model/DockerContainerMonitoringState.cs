using MyLab.LogAgent.LogSourceReaders;

namespace MyLab.LogAgent.Model;

public class DockerContainerMonitoringState
{
    public required DockerContainerInfo Info { get; init; }
    public long Shift { get; set; }
    public List<LogSourceLine> LineBuff { get; } = new();
    public DockerContainerLastIterationParameters LastIteration { get; } = new();
}