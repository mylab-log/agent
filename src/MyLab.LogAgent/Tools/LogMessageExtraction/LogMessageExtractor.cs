namespace MyLab.LogAgent.Tools.LogMessageExtraction;

class LogMessageExtractor : ILogMessageExtractor
{
    private readonly int _messageLenLimit;

    public LogMessageExtractor(int messageLenLimit)
    {
        if (messageLenLimit <= 0)
            throw new ArgumentException("Invalid message len limit");
        _messageLenLimit = messageLenLimit;
    }

    public LogMessage Extract(string originMessage)
    {
        string normOrigin = originMessage.Trim();
        int firstLineIndex = normOrigin.IndexOf('\n');

        string strToTrim = firstLineIndex != -1
            ? normOrigin.Remove(firstLineIndex).TrimEnd()
            : normOrigin;

        if (strToTrim.Length > _messageLenLimit)
        {
            if (strToTrim.Length <= 3)
            {
                return new LogMessage
                {
                    Full = normOrigin,
                    Short = strToTrim.Remove(_messageLenLimit),
                    Shorted = true
                };
            }

            return new LogMessage
            {
                Full = normOrigin,
                Short = strToTrim.Remove(_messageLenLimit) + "...",
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