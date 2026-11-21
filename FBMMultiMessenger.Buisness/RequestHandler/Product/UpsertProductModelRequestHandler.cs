using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Product;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.Product
{
    internal class UpsertProductModelRequestHandler
        : IRequestHandler<UpsertProductModelRequest, BaseResponse<UpsertProductModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public UpsertProductModelRequestHandler(
            ApplicationDbContext dbContext,
            CurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<BaseResponse<UpsertProductModelResponse>> Handle(
            UpsertProductModelRequest request,
            CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();
            if (currentUser is null)
            {
                return BaseResponse<UpsertProductModelResponse>.Error(
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
                return BaseResponse<UpsertProductModelResponse>.Error(
                    "Auto-reply is not included in your plan.",
                    redirectToPackages: true);
            }

            request.CurrentUserId = currentUser.Id;

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BaseResponse<UpsertProductModelResponse>.Error("Please enter a product name.");
            }

            // Listing titles are optional — normalize when provided.
            var listingTitles = ProductQaHelper.DeserializeQuestions(
                ProductQaHelper.SerializeQuestions(request.ListingTitles ?? new()));
            request.ListingTitles = listingTitles;

            if (request.Qas is null || request.Qas.Count == 0)
            {
                return BaseResponse<UpsertProductModelResponse>.Error(
                    "Please add at least one knowledge entry.");
            }

            foreach (var qa in request.Qas)
            {
                var questionsJson = ProductQaHelper.SerializeQuestions(qa.Questions ?? new());
                if (ProductQaHelper.DeserializeQuestions(questionsJson).Count == 0)
                {
                    return BaseResponse<UpsertProductModelResponse>.Error(
                        "Each knowledge entry needs at least one topic.");
                }

                if (string.IsNullOrWhiteSpace(qa.Answer))
                {
                    return BaseResponse<UpsertProductModelResponse>.Error(
                        "Each knowledge entry needs information.");
                }
            }

            var accountIds = (request.SelectedAccounts ?? new())
                .Distinct()
                .ToList();

            if (accountIds.Count > 0)
            {
                var ownedCount = await _dbContext.Accounts
                    .CountAsync(
                        a => a.UserId == request.CurrentUserId && accountIds.Contains(a.Id),
                        cancellationToken);

                if (ownedCount != accountIds.Count)
                {
                    return BaseResponse<UpsertProductModelResponse>.Error(
                        "One or more selected accounts are invalid.");
                }
            }

            if (request.Id is null)
            {
                return await AddAsync(request, accountIds, cancellationToken);
            }

            return await UpdateAsync(request, accountIds, cancellationToken);
        }

        private async Task<BaseResponse<UpsertProductModelResponse>> AddAsync(
            UpsertProductModelRequest request,
            List<int> accountIds,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var product = new Data.Database.DbModels.Product
            {
                UserId = request.CurrentUserId,
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
                ListingTitles = ProductQaHelper.SerializeQuestions(request.ListingTitles),
                IsActive = request.IsActive,
                CreatedAt = now,
                UpdatedAt = now,
            };

            foreach (var qa in request.Qas)
            {
                product.Qas.Add(new ProductQa
                {
                    QuestionVariants = ProductQaHelper.SerializeQuestions(qa.Questions),
                    Answer = qa.Answer.Trim(),
                    IntentKey = string.IsNullOrWhiteSpace(qa.IntentKey) ? null : qa.IntentKey.Trim(),
                    IsActive = qa.IsActive,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            await _dbContext.Products.AddAsync(product, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await AssignAccountsAsync(product.Id, request.CurrentUserId, accountIds, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<UpsertProductModelResponse>.Success(
                "Product added successfully",
                new UpsertProductModelResponse { Id = product.Id });
        }

        private async Task<BaseResponse<UpsertProductModelResponse>> UpdateAsync(
            UpsertProductModelRequest request,
            List<int> accountIds,
            CancellationToken cancellationToken)
        {
            var product = await _dbContext.Products
                .Include(p => p.Qas)
                .FirstOrDefaultAsync(
                    p => p.Id == request.Id && p.UserId == request.CurrentUserId,
                    cancellationToken);

            if (product is null)
            {
                return BaseResponse<UpsertProductModelResponse>.Error("Product does not exist");
            }

            var now = DateTime.UtcNow;
            product.Name = request.Name.Trim();
            product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            product.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
            product.ListingTitles = ProductQaHelper.SerializeQuestions(request.ListingTitles);
            product.IsActive = request.IsActive;
            product.UpdatedAt = now;

            var incomingIds = request.Qas
                .Where(q => q.Id.HasValue)
                .Select(q => q.Id!.Value)
                .ToHashSet();

            var toRemove = product.Qas.Where(q => !incomingIds.Contains(q.Id)).ToList();
            if (toRemove.Count > 0)
            {
                _dbContext.ProductQas.RemoveRange(toRemove);
            }

            foreach (var qa in request.Qas)
            {
                var questionsJson = ProductQaHelper.SerializeQuestions(qa.Questions);
                var answer = qa.Answer.Trim();
                var intent = string.IsNullOrWhiteSpace(qa.IntentKey) ? null : qa.IntentKey.Trim();

                if (qa.Id is int existingId)
                {
                    var entity = product.Qas.FirstOrDefault(q => q.Id == existingId);
                    if (entity is null)
                    {
                        continue;
                    }

                    entity.QuestionVariants = questionsJson;
                    entity.Answer = answer;
                    entity.IntentKey = intent;
                    entity.IsActive = qa.IsActive;
                    entity.UpdatedAt = now;
                }
                else
                {
                    product.Qas.Add(new ProductQa
                    {
                        QuestionVariants = questionsJson,
                        Answer = answer,
                        IntentKey = intent,
                        IsActive = qa.IsActive,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }
            }

            await AssignAccountsAsync(product.Id, request.CurrentUserId, accountIds, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<UpsertProductModelResponse>.Success(
                "Product updated successfully",
                new UpsertProductModelResponse { Id = product.Id });
        }

        private async Task AssignAccountsAsync(
            int productId,
            int userId,
            List<int> accountIds,
            CancellationToken cancellationToken)
        {
            var existingLinks = await _dbContext.AccountProducts
                .Where(ap => ap.ProductId == productId)
                .ToListAsync(cancellationToken);

            var toRemove = existingLinks
                .Where(ap => !accountIds.Contains(ap.AccountId))
                .ToList();
            if (toRemove.Count > 0)
            {
                _dbContext.AccountProducts.RemoveRange(toRemove);
            }

            var existingAccountIds = existingLinks.Select(ap => ap.AccountId).ToHashSet();
            var now = DateTime.UtcNow;

            foreach (var accountId in accountIds)
            {
                if (existingAccountIds.Contains(accountId))
                {
                    continue;
                }

                var owned = await _dbContext.Accounts.AnyAsync(
                    a => a.Id == accountId && a.UserId == userId,
                    cancellationToken);
                if (!owned)
                {
                    continue;
                }

                _dbContext.AccountProducts.Add(new AccountProduct
                {
                    AccountId = accountId,
                    ProductId = productId,
                    CreatedAt = now,
                });
            }
        }
    }
}
