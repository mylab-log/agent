namespace MyLab.LogAgent.Options;

class LogAgentDockerOptions
{
    public string ContainersPath { get; set; } = "/var/lib/log-agent/docker-containers";
    public string SocketUri { get; set; } = "unix:///var/run/docker.sock";

    public string[] WhiteLabels { get; set; } = new[]
    {
        "*"
    };

    public string[]? BlackLabels { get; set; }
}