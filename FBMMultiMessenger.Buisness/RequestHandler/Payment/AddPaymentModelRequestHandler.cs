using FBMMultiMessenger.Buisness.Request.Payment;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FBMMultiMessenger.Buisness.RequestHandler.Payment
{
    internal class AddPaymentModelRequestHandler : IRequestHandler<AddPaymentProofModelRequest, BaseResponse<AddPaymentProofModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AddPaymentModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IWebHostEnvironment webHostEnvironment)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
            this._webHostEnvironment=webHostEnvironment;
        }
        public async Task<BaseResponse<AddPaymentProofModelResponse>> Handle(AddPaymentProofModelRequest request, CancellationToken cancellationToken)
        {
            var pricingTiers = await _dbContext.PricingTiers.ToListAsync(cancellationToken);

            var minAccounts = pricingTiers.Min(x => x.MinAccounts);
            var maxAccounts = pricingTiers.Max(x => x.MaxAccounts);

            var pricingTier = pricingTiers
                                        .FirstOrDefault(x => request.AccountsPurchased >= x.MinAccounts
                                                         &&
                                                        request.AccountsPurchased <= x.MaxAccounts);

            if (pricingTier is null)
            {
                return BaseResponse<AddPaymentProofModelResponse>
                .Error(
                $"The number of accounts you entered is not eligible for any pricing tier. Please select a value between {minAccounts} and {maxAccounts}."
                );

            }

            var accountsPurchased = request.AccountsPurchased;
            var purcahsedPrice = request.PurchasedPrice;


            var perAccountPrice = pricingTier.PricePerAccount;
            var actualPurchasePrice = perAccountPrice * accountsPurchased;

            if (purcahsedPrice < actualPurchasePrice || purcahsedPrice > actualPurchasePrice)
            {
                return BaseResponse<AddPaymentProofModelResponse>
                    .Error
                    (
                     $"The payment amount is incorrect. Please ensure it matches the required price for {accountsPurchased} accounts."
                    );
            }

            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check
            if (currentUser is null)
            {
                return BaseResponse<AddPaymentProofModelResponse>.Error("Invalid Request, please login again to continue.");
            }

            var currentUserId = currentUser.Id;
            var paymentProofImages = request.PaymentImages;

            if (paymentProofImages is null)
            {
                return BaseResponse<AddPaymentProofModelResponse>.Error("Please provide payment proof picture to continue.");
            }

            var latestPaymentProof = await _dbContext.PaymentVerifications
                                                     .Where(x => x.UserId == currentUserId)
                                                     .OrderByDescending(x => x.CreatedAt)
                                                     .FirstOrDefaultAsync(cancellationToken);

            if (latestPaymentProof?.Status == PaymentStatus.Pending)
            {
                return BaseResponse<AddPaymentProofModelResponse>.Error("Your previous payment proof is still under review. Please wait for approval before submitting a new one.\r\n");
            }

            var paymentVerification = new PaymentVerification()
            {
                AccountsPurchased = request.AccountsPurchased,
                PurchasePrice = purcahsedPrice,
                ActualPrice = actualPurchasePrice,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UserId = currentUserId,
                SubmissionNote = request.Note,
            };

            await _dbContext.PaymentVerifications.AddAsync(paymentVerification, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var wwwRootPath = _webHostEnvironment.WebRootPath;
            var paymentVerificationImages = new List<PaymentVerificationImage>();

            foreach (var paymentProofImg in paymentProofImages)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(paymentProofImg.FileName);
                string productPath = @"Images\PaymentProofs\User-" + currentUserId;
                string finalPath = Path.Combine(wwwRootPath, productPath);

                if (!Directory.Exists(finalPath))
                {
                    Directory.CreateDirectory(finalPath);
                }

                using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                {
                    paymentProofImg.CopyTo(fileStream);
                }

                var paymentVerificationImg = new PaymentVerificationImage()
                {
                    FilePath = @"\" + productPath + @"\" + fileName,
                    FileName = fileName,
                    PaymentVerificationId = paymentVerification.Id,
                    UploadedAt = DateTime.UtcNow
                };

                paymentVerificationImages.Add(paymentVerificationImg);
            }

            await _dbContext.PaymentVerificationImages.AddRangeAsync(paymentVerificationImages, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<AddPaymentProofModelResponse>.Success("Payment proof submitted successfully! We'll review and activate your subscription soon.", new AddPaymentProofModelResponse());

        }
    }
}
