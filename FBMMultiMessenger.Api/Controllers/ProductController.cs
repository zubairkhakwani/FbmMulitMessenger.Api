using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Product;
using FBMMultiMessenger.Contracts.Contracts.Product;
using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FBMMultiMessenger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public ProductController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<BaseResponse<GetMyProductsHttpResponse>> GetAll()
        {
            var response = await _mediator.Send(new GetMyProductsModelRequest());
            return _mapper.Map<BaseResponse<GetMyProductsHttpResponse>>(response);
        }

        [Authorize]
        [HttpPost]
        public async Task<BaseResponse<UpsertProductHttpResponse>> Add([FromBody] UpsertProductHttpRequest httpRequest)
        {
            var request = _mapper.Map<UpsertProductModelRequest>(httpRequest);
            var response = await _mediator.Send(request);
            return _mapper.Map<BaseResponse<UpsertProductHttpResponse>>(response);
        }

        [Authorize]
        [HttpPost("{id:int}")]
        public async Task<BaseResponse<UpsertProductHttpResponse>> Edit([FromBody] UpsertProductHttpRequest httpRequest, [FromRoute] int id)
        {
            var request = _mapper.Map<UpsertProductModelRequest>(httpRequest);
            request.Id = id;
            var response = await _mediator.Send(request);
            return _mapper.Map<BaseResponse<UpsertProductHttpResponse>>(response);
        }

        [Authorize]
        [HttpDelete("{id:int}")]
        public async Task<BaseResponse<DeleteProductHttpResponse>> Delete([FromRoute] int id)
        {
            var response = await _mediator.Send(new DeleteProductModelRequest { Id = id });
            return _mapper.Map<BaseResponse<DeleteProductHttpResponse>>(response);
        }

        [Authorize]
        [HttpPost("{id:int}/images")]
        [RequestSizeLimit(35_000_000)] // 30 MB total photos + multipart overhead
        public async Task<BaseResponse<UploadProductImagesHttpResponse>> UploadImages([FromRoute] int id, [FromForm] List<IFormFile> files)
        {
            var response = await _mediator.Send(new UploadProductImagesModelRequest
            {
                ProductId = id,
                Files = files ?? new(),
            });
            return _mapper.Map<BaseResponse<UploadProductImagesHttpResponse>>(response);
        }

        [Authorize]
        [HttpDelete("{id:int}/images/{imageId:int}")]
        public async Task<BaseResponse<DeleteProductImageHttpResponse>> DeleteImage([FromRoute] int id, [FromRoute] int imageId)
        {
            var response = await _mediator.Send(new DeleteProductImageModelRequest
            {
                ProductId = id,
                ImageId = imageId,
            });
            return _mapper.Map<BaseResponse<DeleteProductImageHttpResponse>>(response);
        }
    }
}
