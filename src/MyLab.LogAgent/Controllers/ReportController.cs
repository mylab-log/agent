using Microsoft.AspNetCore.Mvc;
using MyLab.LogAgent.Services;

namespace MyLab.LogAgent.Controllers
{
    [ApiController]
    [Route("report")]
    public class ReportController : ControllerBase
    {
        private readonly IDockerContainerRegistry _dockerContainerRegistry;

        public ReportController(IDockerContainerRegistry dockerContainerRegistry)
        {
            _dockerContainerRegistry = dockerContainerRegistry;
        }
        
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(_dockerContainerRegistry.GetContainers());
        }

        [HttpGet("{container}")]
        public IActionResult Get(string container)
        {
            var foundContainer = _dockerContainerRegistry
                .GetContainers()
                .FirstOrDefault(c => c.Container.Id == container || c.Container.Name == container);
            return foundContainer != null ? Ok(foundContainer) : NotFound($"Container '{container}' not found!");
        }
    }
}
