namespace MyLab.LogAgent.Model;

public class DockerContainerSyncReport
{
    public static readonly DockerContainerSyncReport Empty = new()
    {
        Added = Array.Empty<DockerContainerInfo>(),
        Removed = Array.Empty<DockerContainerInfo>()
    };

    public required IReadOnlyCollection<DockerContainerInfo> Removed { get; set; }
    public required IReadOnlyCollection<DockerContainerInfo> Added { get; set; }
}