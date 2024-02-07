using Microsoft.Extensions.Options;
using Moq;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Options;
using MyLab.LogAgent.Services;

namespace Tests
{
    public class LogRegistrarBehavior
    {
        [Fact]
        public async Task ShouldSendLogBatch()
        {
            //Arrange
            LogRecord[]? capturedRecords = null;

            var registrarTransport = new Mock<ILogRegistrationTransport>();
            registrarTransport.Setup(t => t.RegisterLogsAsync(It.IsAny<IEnumerable<LogRecord>>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<LogRecord>, CancellationToken>((records, _) => capturedRecords = records.ToArray());

            var options = new LogAgentOptions
            {
                OutgoingBufferSize = 2
            };

            var registrar = new LogRegistrar(registrarTransport.Object, new OptionsWrapper<LogAgentOptions>(options));

            var logs = new LogRecord[]
            {
                new() { Message = "foo" },
                new() { Message = "bar" },
                new() { Message = "baz" },
            };

            //Act
            await registrar.RegisterAsync(logs[0]);
            await registrar.RegisterAsync(logs[1]);
            await registrar.RegisterAsync(logs[2]);

            //Assert
            Assert.NotNull(capturedRecords);
            Assert.Equal(2, capturedRecords.Length);
            Assert.Equal("foo", capturedRecords[0].Message);
            Assert.Equal("bar", capturedRecords[1].Message);
        }

        [Fact]
        public async Task ShouldFlush()
        {
            //Arrange
            LogRecord[]? capturedRecords = null;

            var registrarTransport = new Mock<ILogRegistrationTransport>();
            registrarTransport.Setup(t => t.RegisterLogsAsync(It.IsAny<IEnumerable<LogRecord>>(), It.IsAny<CancellationToken>()))
                .Callback<IEnumerable<LogRecord>, CancellationToken>((records, _) => capturedRecords = records.ToArray());

            var options = new LogAgentOptions
            {
                OutgoingBufferSize = 2
            };

            var registrar = new LogRegistrar(registrarTransport.Object, new OptionsWrapper<LogAgentOptions>(options));

            var logs = new LogRecord[]
            {
                new() { Message = "foo" },
                new() { Message = "bar" },
                new() { Message = "baz" },
            };

            await registrar.RegisterAsync(logs[0]);
            await registrar.RegisterAsync(logs[1]);
            await registrar.RegisterAsync(logs[2]);

            //Act
            await registrar.FlushAsync();

            //Assert
            Assert.NotNull(capturedRecords);
            Assert.Single(capturedRecords);
            Assert.Equal("baz", capturedRecords[0].Message);
        }
    }
}
