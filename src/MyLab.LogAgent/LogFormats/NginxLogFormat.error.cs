using System.Text.RegularExpressions;
using MyLab.LogAgent.Model;
using LogLevel = MyLab.LogAgent.Model.LogLevel;

namespace MyLab.LogAgent.LogFormats;

partial class NginxLogFormat
{ 
    static class ErrorLogExtractor
    {
        private const string RegexPattern = """
                                      \[(?<level>\w+)\]\s\d+#\d+:\s\*\d+\s(?<message>[^,]+),\s(?<props>[^$]+)
                                      """;
        public static bool Extract(string logText, LogProperties props, out string message, out LogLevel logLevel)
        {
            logLevel = LogLevel.Undefined;
            var m = Regex.Match(logText, RegexPattern);

            if (!m.Success)
            {
                props.Add(LogPropertyNames.ParsingFailedFlag, "true");
                props.Add(LogPropertyNames.ParsingFailureReason, "nginx-error-log-parser");

                message = logText;
                return false;
            }
            else
            {
                message = m.Groups["message"].Success
                    ? m.Groups["message"].Value
                    : NotFound;

                logLevel = DetermineLogLevel(m.Groups["level"]);

                var groupProps = TryExtractProperties(m);

                props.Add(
                    RequestProp,
                    groupProps != null && groupProps.TryGetValue("request", out var requestStr)
                            ? TryCleanupRequest(requestStr.Trim('"'))
                            : NotFound
                );
                return true;
            }
        }

        private static LogLevel DetermineLogLevel(Group mGroup)
        {
            if (!mGroup.Success) return LogLevel.Undefined;

            switch (mGroup.Value)
            {
                case "debug": return LogLevel.Debug;
                case "info": 
                case "notice": return LogLevel.Info;
                case "warn": return LogLevel.Warning;
                case "error":
                case "crit": 
                case "alert": 
                case "emerg": return LogLevel.Error;
                default: return LogLevel.Undefined;
            }
        }

        private static Dictionary<string, string>? TryExtractProperties(Match m)
        {
            Dictionary<string, string>? groupProps = null;
            if (m.Groups["props"].Success)
            {
                groupProps = m.Groups["props"].Value
                    .Split(',')
                    .Select(v => new { Value = v, Splitter = v.IndexOf(':') })
                    .Where(v => v.Splitter != -1 && v.Splitter != 0)
                    .ToDictionary(
                        v => v.Value.Remove(v.Splitter).Trim(),
                        v => v.Value.Substring(v.Splitter + 1, v.Value.Length - (v.Splitter + 1)).Trim()
                    );
            }

            return groupProps;
        }
    }
}