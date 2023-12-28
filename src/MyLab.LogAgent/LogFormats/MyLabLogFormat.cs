using System.Diagnostics;
using System.Text;
using MyLab.Log;
using MyLab.LogAgent.Model;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace MyLab.LogAgent.LogFormats
{
    class MyLabLogFormat : ILogFormat
    {
        private readonly IDeserializer _deserializer = new DeserializerBuilder()
            .Build();
        private readonly ISerializer _serializer = new SerializerBuilder()
            .WithIndentedSequences()
            .Build();

        public ILogBuilder? CreateBuilder()
        {
            return new MultilineLogBuilder();
        }

        public LogRecord? Parse(string logText)
        {
            var logEntity = ParseYaml(logText);

            var props = new List<KeyValuePair<string, string>>();

            if (logEntity[nameof(LogEntity.Labels)] is YamlMappingNode { NodeType: YamlNodeType.Mapping } logLabels)
            {
                
                foreach (var logEntityLabel in logLabels.Children)
                {
                    props.Add(new KeyValuePair<string, string>(logEntityLabel.Key.ToString(), logEntityLabel.Value.ToString()));
                }
            }

            if (logEntity[nameof(LogEntity.Facts)] is YamlMappingNode { NodeType: YamlNodeType.Mapping } logFacts)
            {

                foreach (var logEntityFact in logFacts.Children)
                {
                    props.Add(new KeyValuePair<string, string>(logEntityFact.Key.ToString(), logEntityFact.Value.ToString()));
                }
            }

            if (logEntity[nameof(LogEntity.Time)] is not { } timeNode)
                throw new FormatException("Time node not found");
            if (logEntity[nameof(LogEntity.Message)] is not { } messageNode)
                throw new FormatException("Message node not found");

            string? exceptionDto = null;

            if (logEntity[nameof(LogEntity.Exception)] is { } exceptionNode)
            {
                exceptionDto = ParseException(exceptionNode);
            }

            return new LogRecord
            {
                Time = DateTime.Parse(timeNode.ToString()),
                Message = messageNode.ToString(),
                Properties = props,
                Exception = exceptionDto
            };
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

        private static YamlNode ParseYaml(string logText)
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
            return logEntity;
        }
    }
}
