﻿using System.Diagnostics;
using System.Text;
using MyLab.Log;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools.LogMessageProc;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using LogLevel = MyLab.LogAgent.Model.LogLevel;

namespace MyLab.LogAgent.LogFormats
{
    class MyLabLogFormat : ILogFormat
    {
        public ILogReader? CreateReader()
        {
            return new MyLabLogReader();
        }

        public LogRecord? Parse(string logText, ILogMessageExtractor messageExtractor)
        {
            var logEntity = ParseYaml(logText);

            var props = new List<LogProperty>();

            string? logLevel = null;

            foreach (var logEntityChild in logEntity.Children)
            {
                var chKey = logEntityChild.Key.ToString();

                switch (chKey)
                {
                    case nameof(LogEntity.Labels):
                    {
                        if (logEntityChild.Value is YamlMappingNode { NodeType: YamlNodeType.Mapping } logLabels)
                        {
                            ProcessLabels(logLabels, props, out var labelLogLevel);
                            logLevel ??= labelLogLevel;
                        }
                    }
                    break;
                    case nameof(LogEntity.Facts):
                    {
                        if (logEntityChild.Value is YamlMappingNode { NodeType: YamlNodeType.Mapping } logFacts)
                        {
                            ProcessFacts(logFacts, props, out var labelLogLevel);
                            logLevel ??= labelLogLevel;
                        }
                    }
                    break;
                    case nameof(LogEntity.Exception):
                    {
                        props.Add(
                            new LogProperty
                            {
                                Name = LogPropertyNames.Exception,
                                Value = ParseException(logEntityChild.Value)
                            });
                        }
                        break;
                }
            }

            if(!logEntity.Children.TryGetValue(nameof(LogEntity.Time), out var timeNode))
                throw new FormatException("Time node not found");
            if (!logEntity.Children.TryGetValue(nameof(LogEntity.Message), out var messageNode))
                throw new FormatException("Message node not found");

            var msg = messageExtractor.Extract(messageNode.ToString());
            if (msg.Shorted)
            {
                props.Add(new()
                {
                    Name = LogPropertyNames.OriginMessage,
                    Value = msg.Full
                });
            }

            return new LogRecord
            {
                Time = DateTime.Parse(timeNode.ToString()),
                Message = msg.Short,
                Properties = props,
                Level = DeserializeLogLevel(logLevel)
            };
        }

        private static void ProcessFacts(YamlMappingNode logFacts, List<LogProperty> props, out string? logLevel)
        {
            logLevel = null;

            foreach (var logEntityFact in logFacts.Children)
            {
                var factKey = logEntityFact.Key.ToString();
                var factVal = logEntityFact.Value.ToString();

                if (factKey == "log-level" || factKey == "log_level")
                {
                    logLevel = factVal;
                    continue;
                }

                props.Add(
                    new LogProperty
                    {
                        Name = factKey,
                        Value = factVal
                    }
                );
            }
        }

        private static void ProcessLabels(YamlMappingNode logLabels, List<LogProperty> props, out string? logLevel)
        {
            logLevel = null;

            foreach (var logEntityLabel in logLabels.Children)
            {
                var labelKey = logEntityLabel.Key.ToString();
                var labelVal = logEntityLabel.Value.ToString();

                if (labelKey == "log-level" || labelKey == "log_level")
                {
                    logLevel = labelVal;
                    continue;
                }

                props.Add(
                    new LogProperty
                    {
                        Name = labelKey,
                        Value = labelVal
                    }
                );
            }
        }

        LogLevel DeserializeLogLevel(string? strVal)
        {
            switch (strVal)
            {
                case "info": return LogLevel.Info;
                case "error": return LogLevel.Error;
                case "warning": return LogLevel.Warning;
                default: return LogLevel.Info;
            }
        }

        private string ParseException(YamlNode exceptionNode)
        {
            var ymlStream = new YamlStream
            {
                new (exceptionNode)
            };

            var sb = new StringBuilder();
            using var writer = new StringWriter(sb);
            {
                ymlStream.Save(writer, false);
            }

            return sb.ToString();
        }

        private static YamlMappingNode ParseYaml(string logText)
        {
            var ymlStream = new YamlStream();

            using var rdr = new StringReader(logText);
            ymlStream.Load(rdr);

            if (ymlStream.Documents.Count == 0)
                throw new FormatException("Yaml document not found");

            if (ymlStream.Documents.Count > 1)
                throw new FormatException("Too many yaml documents in one log record")
                    .AndFactIs("doc-count", ymlStream.Documents.Count);

            var logEntity = ymlStream.Documents[0].RootNode;

            if(logEntity is YamlMappingNode yamlMappingNode)
                return yamlMappingNode;

            throw new FormatException("Yaml document should be YamlMappingNode");
        }
    }
}
