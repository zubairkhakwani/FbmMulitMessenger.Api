using FBMMultiMessenger.Buisness.Request.Product;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.Product
{
    internal class DeleteProductModelRequestHandler
        : IRequestHandler<DeleteProductModelRequest, BaseResponse<DeleteProductModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DeleteProductModelRequestHandler(
            ApplicationDbContext dbContext,
            CurrentUserService currentUserService,
            IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<BaseResponse<DeleteProductModelResponse>> Handle(
            DeleteProductModelRequest request,
            CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (currentUser is null)
            {
                return BaseResponse<DeleteProductModelResponse>.Error(
                    "Invalid request, Please login again to continue");
            }

            var today = DateTime.UtcNow;
            var sub = await _dbContext.Subscriptions
                .AsNoTracking()
                .Where(s =>
                    s.UserId == currentUser.Id
                    && s.StartedAt <= today
                    && s.ExpiredAt > today)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (sub is null || !sub.HasAutoReply)
            {
                return BaseResponse<DeleteProductModelResponse>.Error(
                    "Auto-reply is not included in your plan.",
                    redirectToPackages: true);
            }

            var product = await _dbContext.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(
                    p => p.Id == request.Id && p.UserId == currentUser.Id,
                    cancellationToken);

            if (product is null)
            {
                return BaseResponse<DeleteProductModelResponse>.Error("Product does not exist");
            }

            var links = await _dbContext.AccountProducts
                .Where(ap => ap.ProductId == product.Id)
                .ToListAsync(cancellationToken);
            if (links.Count > 0)
            {
                _dbContext.AccountProducts.RemoveRange(links);
            }

            foreach (var image in product.Images)
            {
                TryDeleteFile(image.RelativePath);
            }

            _dbContext.Products.Remove(product);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<DeleteProductModelResponse>.Success(
                "Product deleted successfully",
                new DeleteProductModelResponse());
        }

        private void TryDeleteFile(string relativePath)
        {
            try
            {
                var full = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(full))
                {
                    File.Delete(full);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }
}
