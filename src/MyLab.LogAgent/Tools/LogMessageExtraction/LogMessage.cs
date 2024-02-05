namespace MyLab.LogAgent.Tools.LogMessageExtraction
{
    public class LogMessage
    {
        public required string Full { get; init; }
        public required string Short { get; init; }
        public bool Shorted { get; init; }
    }
}
