using MyLab.LogAgent.Model;
using Newtonsoft.Json;

namespace MyLab.LogAgent.LogSourceReaders
{
    class DockerLogSourceReader : ILogSourceReader
    {
        private readonly StreamReader _streamReader;

        public bool IgnoreStreamType { get; set; } = false;

        public DockerLogSourceReader(StreamReader streamReader)
        {
            _streamReader = streamReader ?? throw new ArgumentNullException(nameof(streamReader));
        }

        public async Task<LogSourceLine?> ReadLineAsync(CancellationToken cancellationToken)
        {
            if (_streamReader.EndOfStream) return null;

            string? line;
            do
            {
                line = await _streamReader.ReadLineAsync(cancellationToken);
            } while (!_streamReader.EndOfStream && string.IsNullOrWhiteSpace(line));

            if (string.IsNullOrWhiteSpace(line))
                return null;

            DockerLogLine? dockerLine;

            try
            {
                dockerLine = JsonConvert.DeserializeObject<DockerLogLine>(line);
            }
            catch (Exception e)
            {
                throw new SourceLogReadingException("Unable to parse docker json file string", line, e);
            }

            return dockerLine == null
                ? throw new InvalidOperationException("Can't deserialize log string")
                : new LogSourceLine(dockerLine.Log?.TrimEnd('\n', '\r') ?? string.Empty)
            {
                Time = dockerLine.Time,
                Properties = dockerLine.Attributes?.Select(a => new LogProperty
                {
                    Name = a.Key,
                    Value = a.Value
                }),
                IsError = !IgnoreStreamType && dockerLine.Stream == "stderr"
            };
        }

        class DockerLogLine
        {
            [JsonProperty("log")]
            public string? Log { get; set; }
            [JsonProperty("stream")]
            public string? Stream { get; set; }
            [JsonProperty("attrs")]
            public Dictionary<string, string>? Attributes { get; set; }
            [JsonProperty("time")]
            public DateTime? Time { get; set; }
        }
    }
}
