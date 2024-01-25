using Microsoft.Extensions.Options;
using MyLab.LogAgent.Options;

namespace MyLab.LogAgent.Services
{
    public interface IDockerContainerFilesProvider
    {
        IEnumerable<ContainerFile> EnumerateContainerFiles(string containerId);

        StreamReader OpenContainerFileRead(string containerId, string filename);
    }

    public record ContainerFile(string Filename, long Length);

    class DockerContainerFilesProvider(IOptions<LogAgentOptions> options) : IDockerContainerFilesProvider
    {
        private readonly LogAgentOptions _opts = options.Value;
        
        public IEnumerable<ContainerFile> EnumerateContainerFiles(string containerId)
        {
            var containerDirPath = Path.Combine(_opts.DockerContainersPath, containerId);
            
            return new DirectoryInfo(containerDirPath)
                .EnumerateFiles()
                .Select(f => new ContainerFile(f.Name, f.Length));
        }

        public StreamReader OpenContainerFileRead(string containerId, string filename)
        {
            var filePath = Path.Combine(_opts.DockerContainersPath, containerId, filename);

            return File.OpenText(filePath);
        }
    }
}
