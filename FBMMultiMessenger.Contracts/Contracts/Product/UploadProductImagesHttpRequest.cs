namespace FBMMultiMessenger.Contracts.Contracts.Product
{
    public class UploadProductImagesHttpResponse
    {
        public List<ProductImageHttpItem> Images { get; set; } = new();
    }

    public class DeleteProductImageHttpResponse
    {
    }
}
