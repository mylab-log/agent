using MyLab.LogAgent.Tools;

namespace Tests
{
    public class LogMessageBehavior
    {
        [Theory]
        [MemberData(nameof(GetMessageCases))]
        public void ShouldExtractMessage(string origin, string expectedShort, int limit, bool expectedShortedFlag)
        {
            //Arrange

            //Act
            var actualMessage = LogMessage.Extract(origin, limit);

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
