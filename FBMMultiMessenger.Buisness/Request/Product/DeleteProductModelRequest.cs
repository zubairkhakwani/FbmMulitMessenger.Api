using FBMMultiMessenger.Contracts.Shared;
using MediatR;

namespace FBMMultiMessenger.Buisness.Request.Product
{
    public class DeleteProductModelRequest : IRequest<BaseResponse<DeleteProductModelResponse>>
    {
        public int Id { get; set; }
    }

    public class DeleteProductModelResponse
    {
    }
}
