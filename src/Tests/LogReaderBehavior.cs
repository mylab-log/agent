using System.Text;
using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.Tools;

namespace Tests;

public class LogReaderBehavior
{
    [Fact]
    public async Task ShouldReadLogRecord()
    {
        //Arrange
        var lines = new []
        {
            "Start log",
            " line2",
            " line3"
        };

        var sourceString = string.Join(Environment.NewLine, lines);
        var memStream = new MemoryStream(Encoding.UTF8.GetBytes(sourceString));
        var streamReader = new StreamReader(memStream);
        var logFormat = new DefaultLogFormat();

        var reader = new LogReader(logFormat, streamReader, null);

        //Act
        var readLogRecord = await reader.ReadLogAsync(default);

        //Assert
        Assert.NotNull(readLogRecord);
        Assert.Equal(sourceString, readLogRecord.Message);
    }

    [Fact]
    public async Task ShouldBuffNewLogLines()
    {
        //Arrange
        var lines = new[]
        {
            "Start log-1",
            " line2",
            " line3",
            "Start log-2",
            " line2",
            " line3"
        };

        var sourceString = string.Join(Environment.NewLine, lines);
        var fullLog1String = string.Join(Environment.NewLine, lines.Take(3));

        var memStream = new MemoryStream(Encoding.UTF8.GetBytes(sourceString));
        var streamReader = new StreamReader(memStream);
        var logFormat = new DefaultLogFormat();

        var buff = new List<string>();

        var reader = new LogReader(logFormat, streamReader, buff);

        //Act
        var readLogRecord = await reader.ReadLogAsync(default);

        //Assert
        Assert.NotNull(readLogRecord);
        Assert.Equal(fullLog1String, readLogRecord.Message);
        Assert.Single(buff);
        Assert.Equal("Start log-2", buff[0]);
    }

    [Fact]
    public async Task ShouldReadLogWithBuffer()
    {
        //Arrange
        const string stringInBuff = "Start log-2";
        var lines = new[]
        {
            " line2",
            " line3"
        };

        var sourceString = string.Join(Environment.NewLine, lines);
        var fullLog1String = string.Join
        (
            Environment.NewLine, 
            Enumerable.Repeat(stringInBuff, 1).Union(lines)
        );

        var memStream = new MemoryStream(Encoding.UTF8.GetBytes(sourceString));
        var streamReader = new StreamReader(memStream);
        var logFormat = new DefaultLogFormat();

        var buff = new List<string>
        {
            stringInBuff
        };

        var reader = new LogReader(logFormat, streamReader, buff);

        //Act
        var readLogRecord = await reader.ReadLogAsync(default);

        //Assert
        Assert.NotNull(readLogRecord);
        Assert.Equal(fullLog1String, readLogRecord.Message);
        Assert.Empty(buff);
    }

    [Fact]
    public async Task ShouldIgnoreEmptyLines()
    {
        //Arrange
        var lines = new[]
        {
            "Start log-1",
            " line2",
            " line3",
            " ",
            "",
            "\t",
            "Start log-2",
        };

        var sourceString = string.Join(Environment.NewLine, lines);

        var memStream = new MemoryStream(Encoding.UTF8.GetBytes(sourceString));
        var streamReader = new StreamReader(memStream);
        var logFormat = new DefaultLogFormat();

        var buff = new List<string>();

        var reader = new LogReader(logFormat, streamReader, buff);

        await reader.ReadLogAsync(default);

        //Act
        var readLogRecord = await reader.ReadLogAsync(default);

        //Assert
        Assert.NotNull(readLogRecord);
        Assert.Equal("Start log-2", readLogRecord.Message);
    }
}