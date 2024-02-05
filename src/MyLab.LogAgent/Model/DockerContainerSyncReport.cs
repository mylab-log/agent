namespace MyLab.LogAgent.Model;

public class DockerContainerSyncReport
{
    public static readonly DockerContainerSyncReport Empty = new()
    {
        Added = Array.Empty<DockerContainerMonitoringState>(),
        Removed = Array.Empty<DockerContainerMonitoringState>()
    };

    public required IReadOnlyCollection<DockerContainerMonitoringState> Removed { get; set; }
    public required IReadOnlyCollection<DockerContainerMonitoringState> Added { get; set; }
}