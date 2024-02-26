using MyLab.LogAgent;
using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.Model;
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
                               Time: 2023-12-28T13:38:00.000
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
            var logRecord = format.Parse(log, TestTools.DefaultMessageExtractor);

            //Assert
            Assert.NotNull(logRecord);
            Assert.Equal("Something wrong!", logRecord.Message);
            Assert.Equal(new DateTime(2023, 12,28, 13,38,0), logRecord.Time);
            Assert.Equal(DateTimeKind.Local, logRecord.Time.Kind);
            Assert.NotNull(logRecord.Properties);
            Assert.Contains(logRecord.Properties, p => p is { Name: "log-category", Value: "KeslService" });
            Assert.Contains(logRecord.Properties, p => p is { Name: "exception-trace", Value: "07ec3acc398533dd64f43420afda50c5" });
            Assert.Contains(logRecord.Properties, p => p is { Name: "response-dump"} && p.Value.Contains("Transfer-Encoding: chunked"));
            Assert.Contains(logRecord.Properties, p => p is { Name: LogPropertyNames.Exception } && p.Value.Contains("Name not known"));
            Assert.Contains(logRecord.Properties, p => p is { Name: LogPropertyNames.Exception } && p.Value.Contains("Service not known"));
            
            output.WriteLine("PROPERTIES:");
            output.WriteLine("");
            foreach (var logRecordProperty in logRecord.Properties)
            {
                output.WriteLine("");
                output.WriteLine($"NAME => {logRecordProperty.Name}");
                output.WriteLine($"VALUE => {logRecordProperty.Value}");
                output.WriteLine("");
            }
            output.WriteLine("");
        }

        [Theory]
        [MemberData(nameof(GetLogLevelCases))]
        public void ShouldNormalizeLogLevel(string log)
        {
            //Arrange
            var format = new MyLabLogFormat();

            //Act
            var logRecord = format.Parse(log, TestTools.DefaultMessageExtractor);

            //Assert
            Assert.NotNull(logRecord);
            Assert.Equal(LogLevel.Error, logRecord.Level);
        }

        [Fact]
        public void ShouldReadPerOneRecord()
        {
            //Arrange
            var format = new MyLabLogFormat();
            var b = format.CreateReader()!;

            var strings = new []
            {
                "Message: foo",
                "Time: 2024-01-01T01:01:01",
                "\n",
                "Message: bar",
                "Time: 2024-01-01T01:01:01"
            };

            //Act
            foreach (var s in strings)
            {
                var applyRes = b.ApplyNexLine(s);

                if (applyRes != LogReaderResult.Accepted)
                    break;
            }

            var resStr = b.BuildString();
            var rec = format.Parse(resStr.Text, TestTools.DefaultMessageExtractor);

            //Assert
            Assert.NotNull(rec);
            Assert.Equal("foo", rec.Message);
        }

        [Fact]
        public void ShouldProcessNativeNetLog()
        {
            //Arrange
            var lines = new[]
            {
                "\u001b[41m\u001b[30minfo\u001b[41m\u001b[30m: MyApp.Worker[0]",
                "      Hellow!"
            };

            var format = new MyLabLogFormat();

            var rdr = format.CreateReader()!;

            foreach (var line in lines)
            {
                var applyRes = rdr.ApplyNexLine(line);

                if (applyRes != LogReaderResult.Accepted)
                    break;
            }

            var str = rdr.BuildString();

            //Act
            var logRec = format.Parse(str.Text, TestTools.DefaultMessageExtractor);

            //Assert
            Assert.NotNull(logRec);
            Assert.Equal("Hellow!", logRec.Message);
            Assert.NotNull(logRec.Properties);
            Assert.Contains(logRec.Properties, p => p is { Name: LogPropertyNames.Category, Value: "MyApp.Worker" });
        }

        [Fact]
        public void ShouldProcessUnhandledException()
        {
            //Arrange
            var lines = 
                """
                Unhandled exception. System.InvalidOperationException: Remote config loading error
                 ---> System.Net.WebException: Name or service not known (infonot-config:80)
                 ---> System.Net.Http.HttpRequestException: Name or service not known (infonot-config:80)
                 ---> System.Net.Sockets.SocketException (00000005, 0xFFFDFFFF): Name or service not known
                   at System.Net.Dns.GetHostEntryOrAddressesCore(String hostName, Boolean justAddresses, AddressFamily addressFamily, Int64 startingTimestamp)
                   at System.Net.Dns.GetHostAddresses(String hostNameOrAddress, AddressFamily family)
                   at System.Net.Sockets.Socket.Connect(String host, Int32 port)
                   at System.Net.Sockets.Socket.Connect(EndPoint remoteEP)
                   at System.Net.HttpWebRequest.<>c__DisplayClass219_0.<<CreateHttpClient>b__1>d.MoveNext()
                --- End of stack trace from previous location ---
                   at System.Net.Http.HttpConnectionPool.ConnectToTcpHostAsync(String host, Int32 port, HttpRequestMessage initialRequest, Boolean async, CancellationToken cancellationToken)
                   --- End of inner exception stack trace ---
                """
                    .Split("\n");

            var format = new MyLabLogFormat();

            var rdr = format.CreateReader()!;

            foreach (var line in lines)
            {
                var applyRes = rdr.ApplyNexLine(line);

                if (applyRes != LogReaderResult.Accepted)
                    break;
            }

            var str = rdr.BuildString();

            //Act
            var logRec = format.Parse(str.Text, TestTools.DefaultMessageExtractor);

            //Assert
            Assert.NotNull(logRec);
            Assert.StartsWith("Unhandled exception. System.InvalidOperationException: Remote config loading error", logRec.Message);
            Assert.Equal(LogLevel.Error, logRec.Level);
        }

        [Fact]
        public void ShouldReadMultilineMessage()
        {
            //Arrange
            string[] lines = """
                               Message: >-
                                 409(Conflict). {"error":"Conflict","error_description":"Unable to process the requested resource because of conflict in the current state The consent request was already used and can no longer be changed."}
                                 .
                               Time: 2024-02-26T18:29:57.713
                               Labels:
                                 exception-trace: 395862094217b69d7617564327feeb3d
                                 log-level: error
                                 trace-id: a558cd1ea5c241c56a260738c224b865
                               Facts:
                                 log-category: Infonot.Login.Web.Pages.ConsentModel
                               Exception:
                                 Message: >-
                                   409(Conflict). {"error":"Conflict","error_description":"Unable to process the requested resource because of conflict in the current state The consent request was already used and can no longer be changed."}
                                   .
                                 ExceptionTrace: 395862094217b69d7617564327feeb3d
                                 Type: MyLab.ApiClient.ResponseCodeException
                                 StackTrace: >2-
                                      at MyLab.ApiClient.CallDetails.ThrowIfUnexpectedStatusCode()
                                      at MyLab.ApiClient.ApiProxy`1.CallAndObserve[T](MethodInfo targetMethod, Object[] args)
                                      at Infonot.Login.Web.Services.ConcentService.AutoAcceptConsent(String consentChallenge, ConsentRequestDto consentState) in /builds/triasoft/infonot/account-component/src/Infonot.Login.Web/Services/IConsentService.cs:line 166
                                      at Infonot.Login.Web.Pages.ConsentModel.OnPostAsync(String consentChallenge) in /builds/triasoft/infonot/account-component/src/Infonot.Login.Web/Pages/Consent.cshtml.cs:line 231
                               """
                .Split("\n");

            var format = new MyLabLogFormat();

            var rdr = format.CreateReader()!;

            foreach (var line in lines)
            {
                var applyRes = rdr.ApplyNexLine(line);

                if (applyRes != LogReaderResult.Accepted)
                    break;
            }

            var str = rdr.BuildString();

            //Act
            var logRec = format.Parse(str.Text, TestTools.DefaultMessageExtractor);

            //Assert
            Assert.NotNull(logRec);
            Assert.Equal("409(Conflict). {\"error\":\"Conflict\",\"error_description\":\"Unable to process the requested resource because of conflict in the current state The consent request was already used and can no longer be changed.\"} .",logRec.Message);
        }

        public static object[][] GetLogLevelCases()
        {
            return new[]
            {
                new[]
                {
                    """
                   Message: Something wrong!
                   Time: 2023-12-28T13:38:00.000Z
                   Facts:
                     log-level: 'error'
                   Labels:
                     exception-trace: 07ec3acc398533dd64f43420afda50c5
                   """
                },
                new[]
                {
                    """
                    Message: Something wrong!
                    Time: 2023-12-28T13:38:00.000Z
                    Facts:
                      log_level: 'error'
                    Labels:
                      exception-trace: 07ec3acc398533dd64f43420afda50c5
                    """
                },
                new[]
                {
                    """
                    Message: Something wrong!
                    Time: 2023-12-28T13:38:00.000Z
                    Facts:
                      log-category: KeslService
                    Labels:
                      log-level: 'error'
                    """
                },
                new[]
                {
                    """
                    Message: Something wrong!
                    Time: 2023-12-28T13:38:00.000Z
                    Facts:
                      log-category: KeslService
                    Labels:
                      log_level: 'error'
                    """
                }
            };
        }
    }
}
