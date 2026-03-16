using Contracts.ManagerToWorker;
using Manager.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters.Xml.Extensions;

namespace Manager.Api.Controllers
{
    [ApiController]
    [Route("/internal/api/manager/hash/crack")]
    public class InternalController : ControllerBase
    {

        private readonly IRequestFinalizer _requestFinalizer;

        public InternalController(IRequestFinalizer requestFinalizer)
        {
            _requestFinalizer = requestFinalizer;
        }

        [HttpPatch("request")]
        public async Task<IActionResult> ReceiveResponse([FromXmlBody] CrackHashWorkerResponse response)
        {

            await _requestFinalizer.ProcessWorkerResponse(response);

            return Ok(response);

        }

    }
}
