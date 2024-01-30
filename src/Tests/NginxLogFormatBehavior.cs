using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyLab.LogAgent.LogFormats;

namespace Tests
{
    public class NginxLogFormatBehavior
    {
        [Fact]
        public void ShouldParseAccess()
        {
            //Arrange
            const string logLine = """
                                   172.19.0.112 - - [30/Jan/2024:11:14:29 +0300] "GET /metrics HTTP/1.1" 200 2406 "-" "-" "-"
                                   """;

            var format = new NginxLogFormat();

            //Act
            var logRec = format.Parse(logLine, TestTools.DefaultMessageExtractor);

            //Assert
            Assert.NotNull(logRec);
            Assert.NotNull(logRec.Properties);
            Assert.Equal("172.19.0.112: GET /metrics -> 200", logRec.Message);
            Assert.Contains(logRec.Properties, p => p is {Name: NginxLogFormat.RemoteAddressProp, Value: "172.19.0.112"});
            Assert.Contains(logRec.Properties, p => p is {Name: NginxLogFormat.RequestProp, Value: "GET /metrics" });
            Assert.Contains(logRec.Properties, p => p is {Name: NginxLogFormat.StatusProp, Value: "200"});
        }

        [Fact]
        public void ShouldParseError()
        {
            //Arrange
            const string logLine = """
                                   2024/01/30 13:30:52 [error] 18#18: *3690361 open() "/usr/local/openresty/nginx/html/passports/v4/check" failed (2: No such file or directory), client: 172.19.0.1, server: infonot-router, request: "POST /passports/v4/check HTTP/1.1", host: "arm.infonot.ru"
                                   """;

            var format = new NginxLogFormat();

            //Act
            var logRec = format.Parse(logLine, TestTools.DefaultMessageExtractor);

            //Assert
            Assert.NotNull(logRec);
            Assert.NotNull(logRec.Properties);
            Assert.Equal("""
                         open() "/usr/local/openresty/nginx/html/passports/v4/check" failed (2: No such file or directory)
                         """, logRec.Message);
            Assert.Contains(logRec.Properties, p => p is { Name: NginxLogFormat.RequestProp, Value: "POST /passports/v4/check" });
        }
    }
}
