using FBMMultiMessenger.Contracts.Shared;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace FBMMultiMessenger.Buisness.Request.Product
{
    public class UploadProductImagesModelRequest : IRequest<BaseResponse<UploadProductImagesModelResponse>>
    {
        public int ProductId { get; set; }
        public List<IFormFile> Files { get; set; } = new();
    }

    public class UploadProductImagesModelResponse
    {
        public List<ProductImageModelItem> Images { get; set; } = new();
    }

    public class DeleteProductImageModelRequest : IRequest<BaseResponse<DeleteProductImageModelResponse>>
    {
        public int ProductId { get; set; }
        public int ImageId { get; set; }
    }

    public class DeleteProductImageModelResponse
    {
    }
}
