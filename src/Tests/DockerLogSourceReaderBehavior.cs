﻿using System.Text;
using MyLab.LogAgent.LogSourceReaders;
using Xunit.Abstractions;
using StreamReader = System.IO.StreamReader;

namespace Tests
{
    public class DockerLogSourceReaderBehavior
    {
        private readonly ITestOutputHelper _output;

        public DockerLogSourceReaderBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

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
            Assert.Equal("time=\"2024-01-09T08:44:42Z\" level=error msg=\"foo\"",readLine.Text);
            Assert.NotNull(readLine.Time);
            Assert.Equal(new DateTime(2024, 01, 09, 8, 44, 44, DateTimeKind.Utc),readLine.Time);
            Assert.NotNull(readLine.Properties);
            Assert.Contains(readLine.Properties, p => p is { Name: "tag", Value: "redis-monitor" });


            _output.WriteLine("Time: " + readLine.Time.Value.ToString("O"));
        }

        [Fact]
        public async Task ShouldReadEmptyStringsFromDockerLog()
        {
            //Arrange
            const string logLine = """
                                   {"log":"time=\"2024-01-09T08:44:42Z\" level=error msg=\"foo\"\n","stream":"stderr","attrs":{"tag":"redis-monitor"},"time":"2024-01-09T08:44:44.000Z"}
                                   {"log":"\n","stream":"stderr","attrs":{"tag":"redis-monitor"},"time":"2024-01-09T08:44:44.000Z"}
                                   """;
            var mem = new MemoryStream(Encoding.UTF8.GetBytes(logLine));
            var streamReader = new StreamReader(mem);

            var dockerReader = new DockerLogSourceReader(streamReader);
            await dockerReader.ReadLineAsync(default);

            //Act
            var readLine = await dockerReader.ReadLineAsync(default);

            //Assert
            Assert.NotNull(readLine);
            Assert.Equal("", readLine.Text);
        }

        [Theory]
        [MemberData(nameof(GetErrorFactorCases))]
        public async Task ShouldDetectErrorFactor(string logLine, bool expectedFactor)
        {
            //Arrange
            var mem = new MemoryStream(Encoding.UTF8.GetBytes(logLine));
            var streamReader = new StreamReader(mem);

            var dockerReader = new DockerLogSourceReader(streamReader);

            //Act
            var readLine = await dockerReader.ReadLineAsync(default);

            //Assert
            Assert.NotNull(readLine);
            Assert.Equal(expectedFactor, readLine.IsError);
        }

        public static object[][] GetErrorFactorCases()
        {
            return
            [
                [
                    """
                    {"log":"time=\"2024-01-09T08:44:42Z\" level=warning msg=\"foo\"\n","stream":"stderr","attrs":{"tag":"redis-monitor"},"time":"2024-01-09T08:44:44.000Z"}
                    """,
                    true
                ],
                new object[]
                {
                    """
                    {"log":"time=\"2024-01-09T08:44:42Z\" level=warning msg=\"foo\"\n","stream":"stdout","attrs":{"tag":"redis-monitor"},"time":"2024-01-09T08:44:44.000Z"}
                    """,
                    false
                }
            ];
        }
    }
}
