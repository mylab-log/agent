using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools.LogMessageExtraction;
using LogLevel = MyLab.LogAgent.Model.LogLevel;

namespace MyLab.LogAgent.LogFormats
{
    static class NetFormatLogic
    {
        public static LogRecord Parse(string logText, ILogMessageExtractor messageExtractor)
        {
            var props = new List<LogProperty>();

            ExtractAndRemoveLevel(logText, out var level, out var logTextWithoutLevel);
            ExtractCategory(logTextWithoutLevel, out var category, out var leftText);

            props.Add(new LogProperty
            {
                Name = LogPropertyNames.Category,
                Value = category
            });

            var rec = messageExtractor.ExtractAndCreateLogRecord(leftText, props);

            if (level != LogLevel.Undefined)
                rec.Level = level;

            return rec;
        }

        public static void ExtractCategory(string logText, out string category, out string leftText)
        {
            int firstLineIndex = logText.IndexOf('\n');
            if (firstLineIndex == -1)
                throw new FormatException("Empty .NET log message");

            string categoryStr = logText.Remove(firstLineIndex).TrimEnd();

            if (categoryStr[^3] == '[' && char.IsDigit(categoryStr[^2]) && categoryStr[^1] == ']')
            {
                category = categoryStr.Remove(categoryStr.Length - 3);
            }
            else
            {
                category = categoryStr;
            }

            leftText = logText
                .Substring(firstLineIndex)
                .TrimStart('\n', '\r');
        }

        public static void ExtractAndRemoveLevel(string logText, out LogLevel logLevel, out string logTextWithoutLevel)
        {
            if (!logText.StartsWith('\u001b'))
            {
                logLevel = LogLevel.Undefined;
                logTextWithoutLevel = logText.TrimEnd();
                return;
            }

            switch (new string(logText.ToCharArray(), 10, 4))
            {
                case "info":
                    logLevel = LogLevel.Info;
                    break;
                case "fail":
                case "crit":
                    logLevel = LogLevel.Error;
                    break;
                case "dbug":
                    logLevel = LogLevel.Debug;
                    break;
                case "warn":
                    logLevel = LogLevel.Warning;
                    break;
                default:
                    logLevel = LogLevel.Undefined;
                    break;
            }

            int indexOfCategory = logText.IndexOf(':');

            if (indexOfCategory != -1 && logText.Length > indexOfCategory + 2)
            {
                logTextWithoutLevel = logText.Substring(indexOfCategory + 2).TrimEnd();
            }
            else
            {
                logTextWithoutLevel = logText.Substring(31).TrimEnd();
            }
        }
    }
}
