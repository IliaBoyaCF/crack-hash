using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Manager.Api.Controllers
{
    [ApiController]
    [Route("/api/hash")]
    public class ClientController : ControllerBase
    {

        private readonly IManager _manager;

        public ClientController(IManager manager) => _manager = manager;

        [HttpPost("crack")]
        public async Task<JsonResult> Crack([FromBody] CrackRequest crackRequest)
        {
            var reqId = await _manager.RegisterAsync(crackRequest);

            return new JsonResult(
                new
                {
                    requestId = reqId,
                });
        }

        [HttpGet("status")]
        public async Task<IRequestInfo> Status([FromQuery] Guid requestId)
        {
            return await _manager.GetStatusAsync(requestId);
        }

    }

}
