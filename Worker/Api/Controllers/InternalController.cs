using Contracts.ManagerToWorker;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters.Xml.Extensions;
using Worker.Abstractions;

namespace Worker.Api.Controllers;

[ApiController]
[Route("/internal/api/worker/hash/crack/")]
public class InternalController : ControllerBase
{

    private readonly IWorker _worker;

    public InternalController(IWorker worker) => _worker = worker;

    [HttpPost("task")]
    public async Task<IActionResult> GetTaskRequest([FromXmlBody] CrackHashManagerRequest request )
    {
        _worker.Schedule(request);
        return Ok(request);
    }

    [HttpGet("ping")]
    public async Task<IActionResult> PingAlive()
    {
        return Ok();
    }
} 
