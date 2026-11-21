using FBMMultiMessenger.Buisness.Helpers;

using FBMMultiMessenger.Buisness.Request.Product;

using FBMMultiMessenger.Buisness.Service;

using FBMMultiMessenger.Contracts.Shared;

using FBMMultiMessenger.Data.DB;

using MediatR;

using Microsoft.AspNetCore.Hosting;

using Microsoft.EntityFrameworkCore;



namespace FBMMultiMessenger.Buisness.RequestHandler.Product

{

    internal class GetMyProductsModelRequestHandler

        : IRequestHandler<GetMyProductsModelRequest, BaseResponse<GetMyProductsModelResponse>>

    {

        private readonly ApplicationDbContext _dbContext;

        private readonly CurrentUserService _currentUserService;

        private readonly IWebHostEnvironment _webHostEnvironment;



        public GetMyProductsModelRequestHandler(

            ApplicationDbContext dbContext,

            CurrentUserService currentUserService,

            IWebHostEnvironment webHostEnvironment)

        {

            _dbContext = dbContext;

            _currentUserService = currentUserService;

            _webHostEnvironment = webHostEnvironment;

        }



        public async Task<BaseResponse<GetMyProductsModelResponse>> Handle(

            GetMyProductsModelRequest request,

            CancellationToken cancellationToken)

        {

            var currentUser = _currentUserService.GetCurrentUser();

            if (currentUser is null)

            {

                return BaseResponse<GetMyProductsModelResponse>.Error(

                    "Invalid request, Please login again to continue");

            }



            var products = await _dbContext.Products

                .AsNoTracking()

                .Include(p => p.Qas)

                .Include(p => p.Images)

                .Include(p => p.AccountProducts)

                .ThenInclude(ap => ap.Account)

                .Where(p => p.UserId == currentUser.Id)

                .OrderByDescending(p => p.UpdatedAt)

                .ToListAsync(cancellationToken);



            var wwwRoot = _webHostEnvironment.WebRootPath;



            var items = products.Select(p => new ProductModelItem

            {

                Id = p.Id,

                Name = p.Name,

                Description = p.Description,

                Category = p.Category,

                IsActive = p.IsActive,

                ListingTitles = ProductQaHelper.DeserializeQuestions(p.ListingTitles),

                AccountIds = p.AccountProducts.Select(ap => ap.AccountId).ToList(),

                Accounts = p.AccountProducts

                    .Select(ap => ap.Account)

                    .OrderBy(a => a.Name)

                    .Select(a => new ProductAccountModelItem

                    {

                        Id = a.Id,

                        Name = string.IsNullOrWhiteSpace(a.CustomName) ? a.Name : a.CustomName!,

                    })

                    .ToList(),

                Qas = p.Qas

                    .OrderBy(q => q.Id)

                    .Select(q => new ProductQaModelItem

                    {

                        Id = q.Id,

                        IntentKey = q.IntentKey,

                        Questions = ProductQaHelper.DeserializeQuestions(q.QuestionVariants),

                        Answer = q.Answer,

                        IsActive = q.IsActive,

                    })

                    .ToList(),

                Images = p.Images

                    .OrderBy(i => i.SortOrder)

                    .ThenBy(i => i.Id)

                    .Select(i => new ProductImageModelItem

                    {

                        Id = i.Id,

                        RelativePath = i.RelativePath,

                        SortOrder = i.SortOrder,

                        SizeBytes = UploadProductImagesModelRequestHandler.TryFileSize(

                            wwwRoot,

                            i.RelativePath),

                    })

                    .ToList(),

                CreatedAt = p.CreatedAt,

                UpdatedAt = p.UpdatedAt,

            }).ToList();



            return BaseResponse<GetMyProductsModelResponse>.Success(

                "Products retrieved successfully",

                new GetMyProductsModelResponse { Items = items });

        }

    }

}

