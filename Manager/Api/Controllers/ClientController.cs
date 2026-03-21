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

        public ClientController(IManager manager) => _manager = manager;

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
            return requestInfo.ToDto();
        }

    }

}
