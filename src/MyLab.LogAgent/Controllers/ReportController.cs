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
    }
}
