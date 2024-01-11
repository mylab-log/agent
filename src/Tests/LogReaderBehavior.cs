using System.Text;
using MyLab.LogAgent;
using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.LogSourceReaders;
using MyLab.LogAgent.Model;
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
        var srcReader = new AsIsLogSourceReader(streamReader);
        var logFormat = new DefaultLogFormat();

        var reader = new LogReader(logFormat, srcReader, null);

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
        var srcReader = new AsIsLogSourceReader(streamReader);
        var logFormat = new DefaultLogFormat();

        var buff = new List<LogSourceLine>();

        var reader = new LogReader(logFormat, srcReader, buff);

        //Act
        var readLogRecord = await reader.ReadLogAsync(default);

        //Assert
        Assert.NotNull(readLogRecord);
        Assert.Equal(fullLog1String, readLogRecord.Message);
        Assert.Single(buff);
        Assert.Equal("Start log-2", buff[0].Text);
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
        var srcReader = new AsIsLogSourceReader(streamReader);
        var logFormat = new DefaultLogFormat();

        var buff = new List<LogSourceLine>
        {
            new (stringInBuff)
        };

        var reader = new LogReader(logFormat, srcReader, buff);

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
        var srcReader = new AsIsLogSourceReader(streamReader);
        var logFormat = new DefaultLogFormat();

        var buff = new List<LogSourceLine>();

        var reader = new LogReader(logFormat, srcReader, buff);

        await reader.ReadLogAsync(default);

        //Act
        var readLogRecord = await reader.ReadLogAsync(default);

        //Assert
        Assert.NotNull(readLogRecord);
        Assert.Equal("Start log-2", readLogRecord.Message);
    }

    [Fact]
    public async Task ShouldProvideErrorLogRecord()
    {
        //Arrange
        var sourceString = "Start log-1";
        var memStream = new MemoryStream(Encoding.UTF8.GetBytes(sourceString));
        var streamReader = new StreamReader(memStream);
        var srcReader = new AsIsLogSourceReader(streamReader);
        var reader = new LogReader(new BadLogFormat(), srcReader, null);
        
        //Act
        var readLogRecord = await reader.ReadLogAsync(default);

        //Assert
        Assert.NotNull(readLogRecord);
        Assert.Equal("Log parsing error", readLogRecord.Message);
        Assert.NotNull(readLogRecord.Properties);
        Assert.Contains(readLogRecord.Properties, p => p.Name == LogPropertyNames.Exception);
    }

    class BadLogFormat : ILogFormat
    {
        public ILogBuilder? CreateBuilder()
        {
            return new SingleLineLogBuilder();
        }

        public LogRecord Parse(string logText)
        {
            throw new FormatException();
        }
    }
}