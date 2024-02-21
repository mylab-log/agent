using MyLab.LogAgent;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Services;

namespace Tests
{
    public class EsLogRecordBehavior
    {
        [Fact]
        public void ShouldInitFromLogRecord()
        {
            //Arrange
            var dTime = new DateTime(1990, 01, 02, 03, 04, 05, DateTimeKind.Utc);
            var logRecord = new LogRecord
            {
                Message = "foo",
                Time = dTime,
                Properties = new List<LogProperty>
                {
                    new() { Name = "bar-name", Value = "bar-value-1" },
                    new() { Name = "bar-name", Value = "bar-value-2" },
                    new() { Name = "baz-name", Value = "baz-value" }
                }
            };

            //Act
            var esLogRecord = EsLogRecord.FromLogRecord(logRecord);

            //Assert
            Assert.NotNull(esLogRecord);
            Assert.Contains(esLogRecord, p => p is { Key:LogPropertyNames.Message, Value:"foo" });
            Assert.Contains(esLogRecord, p => p is { Key: LogPropertyNames.Time, Value: "1990-01-02T03:04:05.0000000Z" });
            Assert.Contains(esLogRecord, p => p is { Key:"bar-name", Value:"bar-value-1, bar-value-2" });
            Assert.Contains(esLogRecord, p => p is { Key:"baz-name", Value:"baz-value" });
        }

        [Theory]
        [MemberData(nameof(GetNormKeyCases))]
        public void ShouldNormPropertyKey(string originKey, string expectedNormalizedKey)
        {
            //Arrange
            var logRecord = new LogRecord
            {
                Message = "foo",
                Time = default,
                Properties = new List<LogProperty>
                {
                    new() { Name = originKey, Value = "some-value" },
                }
            };

            //Act
            var esLogRecord = EsLogRecord.FromLogRecord(logRecord);

            //Assert
            Assert.NotNull(esLogRecord);
            Assert.Contains(esLogRecord, p => p.Key == expectedNormalizedKey && p.Value == "some-value");
        }

        public static object[][] GetNormKeyCases()
        {
            return new[]
            {
                new object[] { "ololo", "ololo" },
                new object[] { "host", LogPropertyNames.HostAltName },
                new object[] { "obj.param", "obj-param" },
            };
        }
    }
}
