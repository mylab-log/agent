namespace MyLab.LogAgent.LogSourceReaders
{
    public class SourceLogReadingException : Exception
    {
        public string SourceText { get; }

        public SourceLogReadingException(string sourceText, Exception innerException)
            :this("Unable to read log source", sourceText, innerException)
        {
            SourceText = sourceText;
        }

        public SourceLogReadingException(string message, string sourceText, Exception innerException)
            : base(message, innerException)
        {
            SourceText = sourceText;
        }

    }
}
