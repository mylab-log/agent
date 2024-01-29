namespace MyLab.LogAgent.Options
{
    class LogAgentOptions
    {
        public string DockerContainersPath { get; set; } = "/var/lib/log-agent/docker-containers";
        public string DockerUri { get; set; } = "unix:///var/run/docker.sock";
        public int OutgoingBufferSize { get; set; } = 100;
        public Dictionary<string,string>? AddProperties { get; set; }
        public bool ReadFromEnd { get; set; } = true;
        public int MessageLenLimit { get; set; } = 500;
    }
}
