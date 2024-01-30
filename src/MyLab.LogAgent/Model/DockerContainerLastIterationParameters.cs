using MyLab.Log;

namespace MyLab.LogAgent.Model;

public class DockerContainerLastIterationParameters
{
    public string? Filename { get; set; }
    public ExceptionDto? Error { get; set; }
    public DateTime? DateTime { get; set; }
    public int LogCount { get; set; }
}