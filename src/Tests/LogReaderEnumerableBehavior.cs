using System.Text;
using MyLab.LogAgent.LogSourceReaders;
using MyLab.LogAgent.Tools;

namespace Tests;

public class LogReaderEnumerableBehavior
{
    [Fact]
    public void ShouldEnumerateLeaderLines()
    {
        //Arrange
        var leaderLines = new [] { "foo", "bar", "baz" }
            .Select(t => new LogSourceLine(t))
            .ToArray();
        var streamReader = new StreamReader(new MemoryStream());
        var srcReader = new AsIsLogSourceReader(streamReader);

        var e = new LogReaderEnumerable(srcReader, leaderLines);

        //Act
        var resultItems = e.ToBlockingEnumerable().ToArray();

        //Assert
        Assert.Equal(leaderLines.Length, resultItems.Length);
        Assert.Equal("foo", resultItems[0]!.Text);
        Assert.Equal("bar", resultItems[1]!.Text);
        Assert.Equal("baz", resultItems[2]!.Text);
    }

    [Fact]
    public void ShouldEnumerateStreamLines()
    {
        //Arrange
        var streamLines = new[] { "foo", "bar", "baz" }
            .Select(t => new LogSourceLine(t))
            .ToArray();
        var memoryStream = new MemoryStream
        (
            Encoding.UTF8.GetBytes
            (
                string.Join(Environment.NewLine, streamLines.Select(l => l.Text))
            )
        );
        var streamReader = new StreamReader(memoryStream);
        var srcReader = new AsIsLogSourceReader(streamReader);

        var e = new LogReaderEnumerable(srcReader, null);

        //Act
        var resultItems = e.ToBlockingEnumerable().ToArray();

        //Assert
        Assert.Equal(streamLines.Length, resultItems.Length);
        Assert.Equal("foo", resultItems[0]!.Text);
        Assert.Equal("bar", resultItems[1]!.Text);
        Assert.Equal("baz", resultItems[2]!.Text);
    }

    [Fact]
    public void ShouldJoinLeaderAndStreamLines()
    {
        //Arrange
        var leaderLines = new[] { "foo", "bar" }
            .Select(t => new LogSourceLine(t))
            .ToArray();
        var streamLines = new[] { "baz", "qoz" };
        var memoryStream = new MemoryStream
        (
            Encoding.UTF8.GetBytes
            (
                string.Join(Environment.NewLine, streamLines)
            )
        );
        var streamReader = new StreamReader(memoryStream);
        var srcReader = new AsIsLogSourceReader(streamReader);

        var e = new LogReaderEnumerable(srcReader, leaderLines);

        //Act
        var resultItems = e.ToBlockingEnumerable().ToArray();

        //Assert
        Assert.Equal(streamLines.Length + leaderLines.Length, resultItems.Length);
        Assert.Equal("foo", resultItems[0]!.Text);
        Assert.Equal("bar", resultItems[1]!.Text);
        Assert.Equal("baz", resultItems[2]!.Text);
        Assert.Equal("qoz", resultItems[3]!.Text);
    }
}