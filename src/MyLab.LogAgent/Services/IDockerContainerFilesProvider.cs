using Microsoft.Extensions.Options;
using MyLab.LogAgent.Options;

namespace MyLab.LogAgent.Services
{
    public interface IDockerContainerFilesProvider
    {
        IEnumerable<string> EnumerateContainerFiles(string containerId);

        StreamReader OpenContainerFileRead(string containerId, string filename);
    }

    class DockerContainerFilesProvider(IOptions<LogAgentOptions> options) : IDockerContainerFilesProvider
    {
        private readonly LogAgentOptions _opts = options.Value;

        public StreamReader OpenLogFileRead(string containerId, string filename)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> EnumerateContainerFiles(string containerId)
        {
            var containerDirPath = Path.Combine(_opts.DockerContainersPath, containerId);

            return Directory.EnumerateFiles(containerDirPath);
        }

        public StreamReader OpenContainerFileRead(string containerId, string filename)
        {
            var filePath = Path.Combine(_opts.DockerContainersPath, containerId, filename);

            return File.OpenText(filePath);
        }
    }
}
