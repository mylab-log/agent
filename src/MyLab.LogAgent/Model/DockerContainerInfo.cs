namespace MyLab.LogAgent.Model
{
    public class DockerContainerInfo
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public string? LogFormat { get; set; }
        public bool IgnoreStreamType { get; set; }
        public bool Enabled { get; set; } = true;
        public IReadOnlyDictionary<string, string>? Labels { get; set; }
    }
}
