using MyLab.LogAgent.Tools.LogMessageProc;

namespace Tests
{
    public class LogMessageExtractorBehavior
    {
        [Theory]
        [MemberData(nameof(GetMessageCases))]
        public void ShouldExtractMessage(string origin, string expectedShort, int limit, bool expectedShortedFlag)
        {
            //Arrange
            var extractor = new LogMessageExtractor(limit);

            //Act
            var actualMessage = extractor.Extract(origin);

            //Assert
            Assert.Equal(expectedShort, actualMessage.Short);
            Assert.Equal(origin, actualMessage.Full);
            Assert.Equal(expectedShortedFlag, actualMessage.Shorted);
        }

        public static object[][] GetMessageCases()
        {
            return new[]
            {
                new object[]
                {
                    "Foo",
                    "Foo",
                    5,
                    false
                },
                new object[]
                {
                    "FooFooFoo",
                    "FooFo...",
                    5,
                    true
                },
                new object[]
                {
                    "foo",
                    "foo",
                    3,
                    false
                },
                new object[]
                {
                    "foo\nbar",
                    "foo",
                    5,
                    true
                },
                new object[]
                {
                    "foofoofoo\nbar",
                    "foofo...",
                    5,
                    true
                },
            };
        }
    }
}
