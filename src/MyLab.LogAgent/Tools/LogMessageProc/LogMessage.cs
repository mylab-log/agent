using Nest;

namespace MyLab.LogAgent.Tools.LogMessageProc
{
    class LogMessage
    {
        public required string Full { get; init; }
        public required string Short { get; init; }
        public bool Shorted { get; init; }
    }
}
