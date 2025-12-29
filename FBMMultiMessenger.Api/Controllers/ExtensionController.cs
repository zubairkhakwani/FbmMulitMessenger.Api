using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Extension;
using FBMMultiMessenger.Contracts.Contracts.Extension;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExtensionController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly IMediator _mediator;

        public ExtensionController(IMapper mapper, IMediator mediator)
        {
            _mapper=mapper;
            _mediator=mediator;
        }

        //[Authorize]
        [HttpGet("/api/lib/bootstrap/css/bootstrap.min.css")]
        public async Task<BaseResponse<GetEncExntesionContentHttpResponse>> Get([FromQuery] bool UpdateServer)
        {
            BaseResponse<GetEncExtensionContentModelResponse> response = await _mediator.Send(new GetEncExtensionContentModelRequest() { UpdateServer = UpdateServer });

            BaseResponse<GetEncExntesionContentHttpResponse> httpResponse = _mapper.Map<BaseResponse<GetEncExntesionContentHttpResponse>>(response);

            return httpResponse;
        }
    }
}
