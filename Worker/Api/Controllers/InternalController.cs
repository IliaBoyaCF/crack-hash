using Contracts.ManagerToWorker;
using Contracts.WorkerToManager;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters.Xml.Extensions;
using Worker.Abstractions;

namespace Worker.Api.Controllers;

[ApiController]
[Route("/internal/api/worker/hash/crack/")]
public class InternalController : ControllerBase
{

    private readonly IWorker _worker;

    private readonly ILogger<InternalController> _logger;

    public InternalController(IWorker worker, ILogger<InternalController> logger)
    {
        _worker = worker;
        _logger = logger;
    }

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

    [HttpGet("progress")]
    public async Task<IActionResult> GetTaskProgress()
    {
        _logger.LogInformation("Got progress check request");
        (string requestId, int partNumber, int partCount, float progress)? progress = _worker.TaskProgress();
        _logger.LogInformation("Answering for progress check with ({RequestId}, {PartNumber}, {PartCount}, {Progress})", progress?.requestId, progress?.partNumber, progress?.partCount, progress?.progress);
        if (progress == null)
        {
            return NotFound();
        }
        return Ok(new TaskStatusResponse
        {
            RequestId = progress.Value.requestId,
            PartNumber = progress.Value.partNumber,
            PartCount = progress.Value.partCount,
            Progress = progress.Value.progress
        });
    }
} 
