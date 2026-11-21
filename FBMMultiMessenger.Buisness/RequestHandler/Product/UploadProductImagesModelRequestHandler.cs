using FBMMultiMessenger.Buisness.Request.Product;

using FBMMultiMessenger.Buisness.Service;

using FBMMultiMessenger.Contracts.Shared;

using FBMMultiMessenger.Data.Database.DbModels;

using FBMMultiMessenger.Data.DB;

using MediatR;

using Microsoft.AspNetCore.Hosting;

using Microsoft.AspNetCore.Http;

using Microsoft.EntityFrameworkCore;



namespace FBMMultiMessenger.Buisness.RequestHandler.Product

{

    internal class UploadProductImagesModelRequestHandler

        : IRequestHandler<UploadProductImagesModelRequest, BaseResponse<UploadProductImagesModelResponse>>

    {

        public const int MaxImagesPerProduct = 5;

        /// <summary>All photos for a product combined (not per file).</summary>

        public const long MaxBytesTotalPerProduct = 30L * 1024 * 1024;



        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)

        {

            ".jpg", ".jpeg", ".png", ".gif", ".webp",

        };



        private readonly ApplicationDbContext _dbContext;

        private readonly CurrentUserService _currentUserService;

        private readonly IWebHostEnvironment _webHostEnvironment;



        public UploadProductImagesModelRequestHandler(

            ApplicationDbContext dbContext,

            CurrentUserService currentUserService,

            IWebHostEnvironment webHostEnvironment)

        {

            _dbContext = dbContext;

            _currentUserService = currentUserService;

            _webHostEnvironment = webHostEnvironment;

        }



        public async Task<BaseResponse<UploadProductImagesModelResponse>> Handle(

            UploadProductImagesModelRequest request,

            CancellationToken cancellationToken)

        {

            var currentUser = _currentUserService.GetCurrentUser();

            if (currentUser is null)

            {

                return BaseResponse<UploadProductImagesModelResponse>.Error(

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
                return BaseResponse<UploadProductImagesModelResponse>.Error(
                    "Auto-reply is not included in your plan.",
                    redirectToPackages: true);
            }

            var product = await _dbContext.Products

                .Include(p => p.Images)

                .FirstOrDefaultAsync(

                    p => p.Id == request.ProductId && p.UserId == currentUser.Id,

                    cancellationToken);



            if (product is null)

            {

                return BaseResponse<UploadProductImagesModelResponse>.Error("Product does not exist");

            }



            var files = request.Files ?? new List<IFormFile>();

            if (files.Count == 0)

            {

                return BaseResponse<UploadProductImagesModelResponse>.Error("Please select at least one image.");

            }



            if (product.Images.Count + files.Count > MaxImagesPerProduct)

            {

                return BaseResponse<UploadProductImagesModelResponse>.Error(

                    $"A product can have at most {MaxImagesPerProduct} images.");

            }



            foreach (var file in files)

            {

                if (file.Length <= 0)

                {

                    return BaseResponse<UploadProductImagesModelResponse>.Error("One of the files is empty.");

                }



                var ext = Path.GetExtension(file.FileName);

                if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))

                {

                    return BaseResponse<UploadProductImagesModelResponse>.Error(

                        "Only JPG, PNG, GIF, or WEBP images are allowed.");

                }

            }



            var wwwRoot = _webHostEnvironment.WebRootPath;

            var existingBytes = product.Images.Sum(i => TryFileSize(wwwRoot, i.RelativePath));

            var newBytes = files.Sum(f => f.Length);

            if (existingBytes + newBytes > MaxBytesTotalPerProduct)

            {

                return BaseResponse<UploadProductImagesModelResponse>.Error(

                    "All product photos together must be 30 MB or smaller.");

            }



            var relativeDir = Path.Combine("Images", "Products", product.Id.ToString());

            var absoluteDir = Path.Combine(wwwRoot, relativeDir);

            Directory.CreateDirectory(absoluteDir);



            var nextOrder = product.Images.Count == 0

                ? 0

                : product.Images.Max(i => i.SortOrder) + 1;



            var now = DateTime.UtcNow;

            var added = new List<ProductImage>();



            foreach (var file in files)

            {

                var ext = Path.GetExtension(file.FileName);

                var fileName = Guid.NewGuid().ToString("N") + ext;

                var absolutePath = Path.Combine(absoluteDir, fileName);

                await using (var stream = new FileStream(absolutePath, FileMode.Create))

                {

                    await file.CopyToAsync(stream, cancellationToken);

                }



                var relativePath = $"Images/Products/{product.Id}/{fileName}";

                var entity = new ProductImage

                {

                    ProductId = product.Id,

                    RelativePath = relativePath,

                    SortOrder = nextOrder++,

                    CreatedAt = now,

                };

                product.Images.Add(entity);

                added.Add(entity);

            }



            product.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(cancellationToken);



            return BaseResponse<UploadProductImagesModelResponse>.Success(

                "Images uploaded successfully",

                new UploadProductImagesModelResponse

                {

                    Images = added.Select(i => new ProductImageModelItem

                    {

                        Id = i.Id,

                        RelativePath = i.RelativePath,

                        SortOrder = i.SortOrder,

                        SizeBytes = TryFileSize(wwwRoot, i.RelativePath),

                    }).ToList(),

                });

        }



        internal static long TryFileSize(string wwwRoot, string relativePath)

        {

            if (string.IsNullOrWhiteSpace(relativePath))

            {

                return 0;

            }



            try

            {

                var full = Path.Combine(

                    wwwRoot,

                    relativePath.Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(full))

                {

                    return new FileInfo(full).Length;

                }

            }

            catch

            {

                // Best-effort.

            }



            return 0;

        }

    }



    internal class DeleteProductImageModelRequestHandler

        : IRequestHandler<DeleteProductImageModelRequest, BaseResponse<DeleteProductImageModelResponse>>

    {

        private readonly ApplicationDbContext _dbContext;

        private readonly CurrentUserService _currentUserService;

        private readonly IWebHostEnvironment _webHostEnvironment;



        public DeleteProductImageModelRequestHandler(

            ApplicationDbContext dbContext,

            CurrentUserService currentUserService,

            IWebHostEnvironment webHostEnvironment)

        {

            _dbContext = dbContext;

            _currentUserService = currentUserService;

            _webHostEnvironment = webHostEnvironment;

        }



        public async Task<BaseResponse<DeleteProductImageModelResponse>> Handle(

            DeleteProductImageModelRequest request,

            CancellationToken cancellationToken)

        {

            var currentUser = _currentUserService.GetCurrentUser();

            if (currentUser is null)

            {

                return BaseResponse<DeleteProductImageModelResponse>.Error(

                    "Invalid request, Please login again to continue");

            }



            var product = await _dbContext.Products

                .Include(p => p.Images)

                .FirstOrDefaultAsync(

                    p => p.Id == request.ProductId && p.UserId == currentUser.Id,

                    cancellationToken);



            if (product is null)

            {

                return BaseResponse<DeleteProductImageModelResponse>.Error("Product does not exist");

            }



            var image = product.Images.FirstOrDefault(i => i.Id == request.ImageId);

            if (image is null)

            {

                return BaseResponse<DeleteProductImageModelResponse>.Error("Image does not exist");

            }



            try

            {

                var full = Path.Combine(

                    _webHostEnvironment.WebRootPath,

                    image.RelativePath.Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(full))

                {

                    File.Delete(full);

                }

            }

            catch

            {

                // Best-effort.

            }



            _dbContext.ProductImages.Remove(image);

            product.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);



            return BaseResponse<DeleteProductImageModelResponse>.Success(

                "Image deleted successfully",

                new DeleteProductImageModelResponse());

        }

    }

}

