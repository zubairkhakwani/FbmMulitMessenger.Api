using AutoMapper;
using FBMMultiMessenger.Buisness.Helpers;
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
        private readonly ExtensionZipCache _extensionZipCache;
        private readonly IConfiguration _configuration;

        public ExtensionController(IMapper mapper, IMediator mediator, ExtensionZipCache extensionZipCache, IConfiguration configuration)
        {
            _mapper=mapper;
            _mediator=mediator;
            _extensionZipCache = extensionZipCache;
            _configuration = configuration;
        }

        // Public download of the unpacked extension as a ZIP, for users who install manually
        // (chrome://extensions → Developer mode → Load unpacked) without FBM Robo.
        [HttpGet("/api/extension/download")]
        public async Task<IActionResult> Download()
        {
            var zipBytes = await _extensionZipCache.GetAsync();

            return File(zipBytes, "application/zip", "fbm-messenger-extension.zip");
        }

        //[Authorize]
        [HttpGet("/api/lib/bootstrap/css/bootstrap.min.css")]
        public async Task<BaseResponse<GetEncExntesionContentHttpResponse>> Get()
        {
            BaseResponse<GetEncExtensionContentModelResponse> response = await _mediator.Send(new GetEncExtensionContentModelRequest());

            BaseResponse<GetEncExntesionContentHttpResponse> httpResponse = _mapper.Map<BaseResponse<GetEncExntesionContentHttpResponse>>(response);

            return httpResponse;
        }

        //[Authorize]
        [HttpGet("/api/lib/bootstrap/js/bootstrap.bundle.min.js")]
        public async Task<BaseResponse<GetEncExntesionContentHttpResponse>> Update()
        {
            BaseResponse<GetEncExtensionContentModelResponse> response = await _mediator.Send(new UpdateExtensionContentRequest());

            BaseResponse<GetEncExntesionContentHttpResponse> httpResponse = _mapper.Map<BaseResponse<GetEncExntesionContentHttpResponse>>(response);

            return httpResponse;
        }

        //[Authorize]
        [HttpGet("/api/lib/bootstrap/css/bootstrap.reboot.min.css")]
        public async Task<BaseResponse<GetExtensionVersionHttpResponse>> GetVersion()
        {
            BaseResponse<GetExtensionVersionModelResponse> response = await _mediator.Send(new GetExtensionVersionRequest());

            BaseResponse<GetExtensionVersionHttpResponse> httpResponse = _mapper.Map<BaseResponse<GetExtensionVersionHttpResponse>>(response);

            return httpResponse;
        }
    }
}
