using MyLab.LogAgent.LogFormats;
using Xunit.Abstractions;

namespace Tests
{
    public class MyLabLogFormatBehavior(ITestOutputHelper output)
    {
        [Fact]
        public void ShouldDeserializeMyLabLog()
        {
            //Arrange
            const string log = """
                               Message: Something wrong!
                               Time: 2023-12-28T13:38:00.000Z
                               Exception:
                                 Message: Name not known
                                 Type: System.Net.Http.HttpRequestException
                                 Inner:
                                   Message: Service not known
                                   Type: System.Net.Sockets.SocketException
                                   StackTrace: '   at System.Net.Http.ConnectHelper.ConnectAsync(String host, Int32
                                     port, CancellationToken cancellationToken)'
                                 StackTrace: |2- 
                                   at System.Net.Http.ConnectHelper.ConnectAsync(String host, Int32 port, CancellationToken cancellationToken)
                                   at System.Net.Http.HttpConnectionPool.ConnectAsync(HttpRequestMessage request, Boolean allowHttp2, CancellationToken cancellationToken)
                                   at System.Net.Http.HttpConnectionPool.CreateHttp11ConnectionAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                                   at System.Net.Http.HttpConnectionPool.GetHttpConnectionAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                                   at System.Net.Http.HttpConnectionPool.SendWithRetryAsync(HttpRequestMessage request, Boolean doRequestAuth, CancellationToken cancellationToken)
                                   at System.Net.Http.RedirectHandler.SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                                   at Microsoft.Extensions.Http.Logging.LoggingHttpMessageHandler.SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                                   at Microsoft.Extensions.Http.Logging.LoggingScopeHttpMessageHandler.SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                                   at System.Net.Http.HttpClient.FinishSendAsyncBuffered(Task`1 sendTask, HttpRequestMessage request, CancellationTokenSource cts, Boolean disposeCts)
                                   at MyLab.TaskKicker.TaskKickerService.KickAsync(KickOptions opts) in /src/MyLab.TaskKicker/TaskKickerService.cs:line 31
                                   at MyLab.TaskKicker.KickTaskJob.Execute(IJobExecutionContext context) in /src/MyLab.TaskKicker/KickTaskJob.cs:line 41
                               Facts:
                                 outputStandardError: ''
                                 outputStandard: ''
                                 exit code: 0
                                 log-category: KeslService
                                 response-dump: |+
                                   200 OK
                                   
                                   Server: nginx/1.20.1
                                   Date: Thu, 28 Dec 2023 16:00:01 GMT
                                   Transfer-Encoding: chunked
                                   Connection: keep-alive
                                   api-supported-versions: 2.0
                                   Content-Type: application/json; charset=utf-8
                                   
                                   {"orderStatuses":[{"bookId":"4bb656d4-8baa-4082-be83-079997233396","notaryAction":"502000","status":"Canceled","comment":"Отменено заявителем","modifiedDate":"2023-12-28T13:00:36.604702Z","hasAttachment":false}]}
                               Labels:
                                 exception-trace: 07ec3acc398533dd64f43420afda50c5
                               """;

            var format = new MyLabLogFormat();

            //Act
            var logRecord = format.Parse(log);

            //Assert
            Assert.NotNull(logRecord);
            Assert.Equal("Something wrong!", logRecord.Message);
            Assert.Equal(new DateTime(2023, 12,28, 13,38,0), logRecord.Time.ToUniversalTime());
            Assert.NotNull(logRecord.Exception);
            Assert.Contains("Name not known", logRecord.Exception);
            Assert.Contains("Service not known", logRecord.Exception);
            Assert.NotNull(logRecord.Properties);
            Assert.Contains(logRecord.Properties, p => p is { Key: "log-category", Value: "KeslService" });
            Assert.Contains(logRecord.Properties, p => p is { Key: "exception-trace", Value: "07ec3acc398533dd64f43420afda50c5" });
            Assert.Contains(logRecord.Properties, p => p is { Key: "response-dump"} && p.Value.Contains("Transfer-Encoding: chunked"));

            output.WriteLine("EXCEPTION:");
            output.WriteLine("");
            output.WriteLine(logRecord.Exception);
            output.WriteLine("");

            output.WriteLine("PROPERTIES:");
            output.WriteLine("");
            foreach (var logRecordProperty in logRecord.Properties)
            {
                output.WriteLine("");
                output.WriteLine($"KEY => {logRecordProperty.Key}");
                output.WriteLine($"VALUE => {logRecordProperty.Value}");
                output.WriteLine("");
            }
            output.WriteLine("");
        }
    }
}
