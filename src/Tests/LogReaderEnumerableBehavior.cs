using System.Text;
using MyLab.LogAgent.Tools;

namespace Tests;

public class LogReaderEnumerableBehavior
{
    [Fact]
    public void ShouldEnumerateLeaderLines()
    {
        //Arrange
        var leaderLines = new [] { "foo", "bar", "baz" };
        var streamReader = new StreamReader(new MemoryStream());

        var e = new LogReaderEnumerable(streamReader, leaderLines);

        //Act
        var resultItems = e.ToBlockingEnumerable().ToArray();

        //Assert
        Assert.Equal(leaderLines.Length, resultItems.Length);
        Assert.Equal("foo", resultItems[0]);
        Assert.Equal("bar", resultItems[1]);
        Assert.Equal("baz", resultItems[2]);
    }

    [Fact]
    public void ShouldEnumerateStreamLines()
    {
        //Arrange
        var streamLines = new[] { "foo", "bar", "baz" };
        var memoryStream = new MemoryStream
        (
            Encoding.UTF8.GetBytes
            (
                string.Join(Environment.NewLine, streamLines)
            )
        );
        var streamReader = new StreamReader(memoryStream);

        var e = new LogReaderEnumerable(streamReader, null);

        //Act
        var resultItems = e.ToBlockingEnumerable().ToArray();

        //Assert
        Assert.Equal(streamLines.Length, resultItems.Length);
        Assert.Equal("foo", resultItems[0]);
        Assert.Equal("bar", resultItems[1]);
        Assert.Equal("baz", resultItems[2]);
    }

    [Fact]
    public void ShouldJoinLeaderAndStreamLines()
    {
        //Arrange
        var leaderLines = new[] { "foo", "bar" };
        var streamLines = new[] { "baz", "qoz" };
        var memoryStream = new MemoryStream
        (
            Encoding.UTF8.GetBytes
            (
                string.Join(Environment.NewLine, streamLines)
            )
        );
        var streamReader = new StreamReader(memoryStream);

        var e = new LogReaderEnumerable(streamReader, leaderLines);

        //Act
        var resultItems = e.ToBlockingEnumerable().ToArray();

        //Assert
        Assert.Equal(streamLines.Length + leaderLines.Length, resultItems.Length);
        Assert.Equal("foo", resultItems[0]);
        Assert.Equal("bar", resultItems[1]);
        Assert.Equal("baz", resultItems[2]);
        Assert.Equal("qoz", resultItems[3]);
    }
}