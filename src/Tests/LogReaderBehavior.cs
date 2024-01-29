﻿using System.Text;
using Microsoft.Extensions.ObjectPool;
using MyLab.LogAgent;
using MyLab.LogAgent.LogFormats;
using MyLab.LogAgent.LogSourceReaders;
using MyLab.LogAgent.Model;
using MyLab.LogAgent.Tools;
using MyLab.LogAgent.Tools.LogMessageProc;
using YamlDotNet.RepresentationModel;

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

        var reader = new LogReader(logFormat, TestTools.DefaultMessageExtractor, srcReader, null);

        //Act
        var readLogRecord = await reader.ReadLogAsync(default);

        //Assert
        Assert.NotNull(readLogRecord);
        Assert.Equal("Start log", readLogRecord.Message);
        Assert.NotNull(readLogRecord.Properties);
        Assert.Contains(readLogRecord.Properties, p => p.Name == LogPropertyNames.OriginMessage && p.Value == sourceString);
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

        var reader = new LogReader(logFormat, TestTools.DefaultMessageExtractor, srcReader, buff);

        //Act
        var readLogRecord = await reader.ReadLogAsync(default);

        //Assert
        Assert.NotNull(readLogRecord);
        Assert.Equal("Start log-1", readLogRecord.Message);
        Assert.NotNull(readLogRecord.Properties);
        Assert.Contains(readLogRecord.Properties, p => p.Name == LogPropertyNames.OriginMessage && p.Value == fullLog1String );
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

        var reader = new LogReader(logFormat, TestTools.DefaultMessageExtractor, srcReader, buff);

        //Act
        var readLogRecord = await reader.ReadLogAsync(default);

        //Assert
        Assert.NotNull(readLogRecord);
        Assert.Equal("Start log-2", readLogRecord.Message);
        Assert.NotNull(readLogRecord.Properties);
        Assert.Contains(readLogRecord.Properties, p => p.Name == LogPropertyNames.OriginMessage && p.Value == fullLog1String);
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

        var reader = new LogReader(logFormat, TestTools.DefaultMessageExtractor, srcReader, buff);

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
        var reader = new LogReader(new BadLogFormat(), TestTools.DefaultMessageExtractor, srcReader, null);
        
        //Act
        var readLogRecord = await reader.ReadLogAsync(default);

        //Assert
        Assert.NotNull(readLogRecord);
        Assert.Equal("Start log-1", readLogRecord.Message);
        Assert.NotNull(readLogRecord.Properties);
        Assert.Contains(readLogRecord.Properties, p => p.Name == LogPropertyNames.Exception);
        Assert.Contains(readLogRecord.Properties, p => p is { Name: LogPropertyNames.ParsingFailedFlag, Value: "true" });
    }

    [Theory]
    [MemberData(nameof(GetErrorFactorCases))]
    public async Task ShouldSetErrorFactorIfNotDefined(string log, bool expectedErrorFactor)
    {
        var memStream = new MemoryStream(Encoding.UTF8.GetBytes(log));
        var streamReader = new StreamReader(memStream);
        var srcReader = new DockerLogSourceReader(streamReader);
        var reader = new LogReader(new MyLabLogFormat(), TestTools.DefaultMessageExtractor, srcReader, null);

        //Act
        var readLogRecord = await reader.ReadLogAsync(default);

        //Assert
        Assert.NotNull(readLogRecord);
        Assert.Equal(expectedErrorFactor, readLogRecord.Level == LogLevel.Error);
    }

    public static object[][] GetErrorFactorCases()
    {
        return new[]
        {
            new[]
            {
                WrapToDockerFormat(
                    """
                    Message: Something wrong!
                    Time: 2023-12-28T13:38:00.000Z
                    Facts:
                      log-category: KeslService
                    Labels:
                      log_level: 'error'
                    """
                    , "stdout"),
                (object)true
            },
            new[]
            {
                WrapToDockerFormat(
                    """
                    Message: Something wrong!
                    Time: 2023-12-28T13:38:00.000Z
                    Facts:
                      log-category: KeslService
                    Labels:
                      log_level: 'error'
                    """
                    , "stderr"),
                (object)true
            },
            new[]
            {
                WrapToDockerFormat(
                    """
                    Message: Something wrong!
                    Time: 2023-12-28T13:38:00.000Z
                    Facts:
                      log-category: KeslService
                    Labels:
                      log_level: 'info'
                    """
                    , "stdout"),
                (object)false
            }
            ,
            new[]
            {
                WrapToDockerFormat(
                    """
                    Message: Something wrong!
                    Time: 2023-12-28T13:38:00.000Z
                    Facts:
                      log-category: KeslService
                    Labels:
                      log_level: 'info'
                    """
                    , "stderr"),
                (object)false
            }
        };
    }

    static string WrapToDockerFormat(string lines, string stream)
    {
        var wrappedStrings= lines
            .Split("\n")
            .Select(l => $"{{\"log\":\"{l.TrimEnd()}\\n\",\"stream\":\"{stream}\",\"time\":\"2023-08-29T20:15:41.304555874Z\"}}");
        return string.Join(Environment.NewLine, wrappedStrings);
    }

    class BadLogFormat : ILogFormat
    {
        public ILogReader? CreateReader()
        {
            return new SingleLineLogReader();
        }

        public LogRecord Parse(string logText, ILogMessageExtractor messageExtractor)
        {
            throw new FormatException();
        }
    }
}