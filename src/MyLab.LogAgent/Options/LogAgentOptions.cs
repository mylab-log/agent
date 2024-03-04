namespace MyLab.LogAgent.Options
{
    class LogAgentOptions
    {
        public LogAgentDockerOptions Docker { get; set; } = new LogAgentDockerOptions();
        public int OutgoingBufferSize { get; set; } = 100;
        public Dictionary<string,string>? AddProperties { get; set; }
        public bool ReadFromEnd { get; set; } = true;
        public int MessageLenLimit { get; set; } = 500;
        public bool UseSourceDt { get; set; } = true;
    }
}
