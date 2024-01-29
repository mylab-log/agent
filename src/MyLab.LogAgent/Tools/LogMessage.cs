using Nest;

namespace MyLab.LogAgent.Tools
{
    class LogMessage
    {
        public required string Full { get; init; }
        public required string Short { get; init; }
        public bool Shorted { get; init; }

        public static LogMessage Extract(string origin, int messageLenLimit)
        {
            if (messageLenLimit <= 0)
                throw new ArgumentException("Invalid message len limit");

            string normOrigin = origin.Trim();
            int firstLineIndex = normOrigin.IndexOf('\n');

            string strToTrim = firstLineIndex != -1
                ? normOrigin.Remove(firstLineIndex).TrimEnd()
                : normOrigin;

            if (strToTrim.Length > messageLenLimit)
            {
                if (strToTrim.Length <= 3)
                {
                    return new LogMessage
                    {
                        Full = normOrigin,
                        Short = strToTrim.Remove(messageLenLimit),
                        Shorted = true
                    };
                }

                return new LogMessage
                {
                    Full = normOrigin,
                    Short = strToTrim.Remove(messageLenLimit) + "...",
                    Shorted = true
                };
            }

            return new LogMessage
            {
                Short = strToTrim,
                Full = normOrigin,
                Shorted = firstLineIndex != -1
            };
        }
    }
}
