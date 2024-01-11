using System.Text;
using MyLab.LogAgent.LogSourceReaders;
using StreamReader = System.IO.StreamReader;

namespace Tests
{
    public class DockerLogSourceReaderBehavior
    {
        [Fact]
        public async Task ShouldReadDockerLog()
        {
            //Arrange
            const string logLine = """
                                   {"log":"time=\"2024-01-09T08:44:42Z\" level=error msg=\"foo\"\n","stream":"stderr","attrs":{"tag":"redis-monitor"},"time":"2024-01-09T08:44:44.000Z"}
                                   """;
            var mem = new MemoryStream(Encoding.UTF8.GetBytes(logLine));
            var streamReader = new StreamReader(mem);

            var dockerReader = new DockerLogSourceReader(streamReader);

            //Act
            var readLine = await dockerReader.ReadLineAsync(default);

            //Assert
            Assert.NotNull(readLine);
            Assert.Equal("time=\"2024-01-09T08:44:42Z\" level=error msg=\"foo\"\n",readLine.Text);
            Assert.Equal(new DateTime(2024, 01, 09, 8, 44, 44),readLine.Time);
            Assert.NotNull(readLine.Properties);
            Assert.Contains(readLine.Properties, p => p is { Name: "tag", Value: "redis-monitor" });
        }
    }
}
