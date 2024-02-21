using Xunit.Abstractions;

namespace Tests
{
    public class DateTimeBehavior
    {
        private readonly ITestOutputHelper _output;

        public DateTimeBehavior(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void ShouldParseDtAsUnspecified()
        {
            //Arrange
            

            //Act
            var dt = DateTime.Parse("01-01-2001 01:01:01");

            //Assert
            Assert.Equal(DateTimeKind.Unspecified, dt.Kind);
        }

        [Fact]
        public void ShouldCreateLocalDtFormUnspecified()
        {
            //Arrange
            var originDt = new DateTime(2001, 1, 1, 1, 1, 1, DateTimeKind.Unspecified);
            var currentOffset = DateTimeOffset.Now.Offset;

            //Act
            var dt = new DateTimeOffset(originDt.Add(currentOffset), currentOffset).LocalDateTime;

            //Assert
            Assert.Equal(DateTimeKind.Local, dt.Kind);
            Assert.Equal(originDt.Add(currentOffset), dt);
        }

        [Theory]
        [MemberData(nameof(GetSerializeCases))]
        public void ShouldSerializeLocal(string testDt, string expectedString)
        {
            //Arrange
            
            //Act

            //Assert
            Assert.Equal(expectedString, testDt);
        }

        [Fact]
        public void ShouldConvertUnspecifiedToLocal()
        {
            //Arrange
            var originDt = new DateTime(2001, 1, 1, 1, 1, 1);

            //Act
            var convertedDt = DateTime.SpecifyKind(originDt, DateTimeKind.Local);

            //Assert
            Assert.Equal(originDt, convertedDt);
            Assert.Equal(DateTimeKind.Unspecified, originDt.Kind);
            Assert.Equal(DateTimeKind.Local, convertedDt.Kind);
            Assert.Equal(DateTimeOffset.Now.Offset, new DateTimeOffset(convertedDt).Offset);

            _output.WriteLine("Origin: " + originDt.ToString("O"));
            _output.WriteLine("Converted: " + convertedDt.ToString("O"));
            _output.WriteLine("Offset: " + DateTimeOffset.Now.Offset);
        }

        [Fact]
        public void NowShouldBeLocal()
        {
            //Arrange
            var dt = DateTime.Now;

            //Act

            //Assert
            Assert.Equal(DateTimeKind.Local, dt.Kind);
            Assert.Equal(DateTimeOffset.Now.Offset, new DateTimeOffset(dt).Offset);

            _output.WriteLine("Time: " + dt.ToString("O"));
            _output.WriteLine("Offset: " + DateTimeOffset.Now.Offset);
        }

        public static object[][] GetSerializeCases()
        {
            var testDt = new DateTime(2001, 1, 1, 1, 1, 1);

            return new object[][]
            {
                new object[]
                {
                    testDt.ToString("O"),
                    "2001-01-01T01:01:01.0000000"
                },
                new object[]
                {
                  new DateTimeOffset(testDt, TimeSpan.Zero).ToString("O"),
                    "2001-01-01T01:01:01.0000000+00:00"
                },
                new object[]
                {
                    DateTime.SpecifyKind(testDt, DateTimeKind.Utc).ToString("O"),
                    "2001-01-01T01:01:01.0000000Z"
                }
            };
        }
    }
}
