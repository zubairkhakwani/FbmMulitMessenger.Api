using AutoMapper;
using FBMMultiMessenger.Buisness.Request.Product;
using FBMMultiMessenger.Contracts.Contracts.Product;
using FBMMultiMessenger.Contracts.Shared;

namespace FBMMultiMessenger.Buisness.Mapping.Product
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<GetMyProductsHttpRequest, GetMyProductsModelRequest>();
            CreateMap<ProductImageModelItem, ProductImageHttpItem>();
            CreateMap<ProductQaModelItem, ProductQaHttpItem>();
            CreateMap<ProductAccountModelItem, ProductAccountHttpItem>();
            CreateMap<ProductModelItem, ProductHttpItem>();
            CreateMap<GetMyProductsModelResponse, GetMyProductsHttpResponse>();
            CreateMap<BaseResponse<GetMyProductsModelResponse>, BaseResponse<GetMyProductsHttpResponse>>();

            CreateMap<UpsertProductQaHttpItem, UpsertProductQaModelItem>();
            CreateMap<UpsertProductHttpRequest, UpsertProductModelRequest>();
            CreateMap<UpsertProductModelResponse, UpsertProductHttpResponse>();
            CreateMap<BaseResponse<UpsertProductModelResponse>, BaseResponse<UpsertProductHttpResponse>>();

            CreateMap<DeleteProductModelResponse, DeleteProductHttpResponse>();
            CreateMap<BaseResponse<DeleteProductModelResponse>, BaseResponse<DeleteProductHttpResponse>>();

            CreateMap<UploadProductImagesModelResponse, UploadProductImagesHttpResponse>();
            CreateMap<BaseResponse<UploadProductImagesModelResponse>, BaseResponse<UploadProductImagesHttpResponse>>();

            CreateMap<DeleteProductImageModelResponse, DeleteProductImageHttpResponse>();
            CreateMap<BaseResponse<DeleteProductImageModelResponse>, BaseResponse<DeleteProductImageHttpResponse>>();
        }
    }
}
