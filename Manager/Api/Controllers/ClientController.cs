using Manager.Abstractions.Model;
using Manager.Abstractions.Services;
using Manager.Api.Dtos;
using Manager.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Manager.Api.Controllers
{
    [ApiController]
    [Route("/api/hash")]
    public class ClientController : ControllerBase
    {

        private readonly IManager _manager;
        private readonly IRequestProgressService _requestProgressService;

        public ClientController(IManager manager, IRequestProgressService requestProgressService)
        {
            _manager = manager;
            _requestProgressService = requestProgressService;
        }

        [HttpPost("crack")]
        public async Task<JsonResult> Crack([FromBody] CrackRequest crackRequest)
        {
            try
            {
                var reqId = await _manager.RegisterAsync(crackRequest);

                return new JsonResult(
                    new
                    {
                        requestId = reqId,
                    });
            }
            catch (OverflowException)
            {
                throw new QueueOverflowException("Server busy. No available place in queue.");
            }
        }

        [HttpGet("status")]
        public async Task<RequestInfoDto> Status([FromQuery] Guid requestId)
        {
            var requestInfo = await _manager.GetStatusAsync(requestId);
            float progress = await _requestProgressService.GetProgressAsync(requestId);
            return requestInfo.ToDto(progress);
        }

    }

}
