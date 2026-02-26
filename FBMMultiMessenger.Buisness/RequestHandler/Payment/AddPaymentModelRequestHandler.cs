using FBMMultiMessenger.Buisness.Request.Payment;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Extensions;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.Payment
{
    internal class AddPaymentModelRequestHandler : IRequestHandler<AddPaymentProofModelRequest, BaseResponse<AddPaymentProofModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IEmailService _emailService;

        public AddPaymentModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService, IWebHostEnvironment webHostEnvironment, IEmailService emailService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
            this._webHostEnvironment=webHostEnvironment;
            this._emailService=emailService;
        }
        public async Task<BaseResponse<AddPaymentProofModelResponse>> Handle(AddPaymentProofModelRequest request, CancellationToken cancellationToken)
        {
            var pricingTier = await _dbContext.PricingTiers
                                              .FirstOrDefaultAsync(p => p.Id == request.PricingTierId, cancellationToken);

            if (pricingTier is null)
            {
                return BaseResponse<AddPaymentProofModelResponse>.Error($"Invalid pricing tier selected. Please choose a valid plan and try again.");
            }

            if (!Enum.TryParse<BillingCylce>(request.BillingCylce.ToString(), out var value))
            {
                return BaseResponse<AddPaymentProofModelResponse>.Error($"Please select a valid billing plan");
            }

            var months = request.BillingCylce switch
            {
                BillingCylce.Monthly => 1,
                BillingCylce.SemiAnnual => 6,
                BillingCylce.Annual => 12,
                _ => 1
            };

            var basePricePerMonth = pricingTier.MonthlyPrice;
            var actualPurchasePrice = basePricePerMonth * months;

            var userPurchasedPrice = request.BillingCylce switch
            {
                BillingCylce.Monthly => pricingTier.MonthlyPrice,
                BillingCylce.SemiAnnual => pricingTier.SemiAnnualPrice,
                BillingCylce.Annual => pricingTier.AnnualPrice,
                _ => 1
            };

            userPurchasedPrice = userPurchasedPrice * months;

            if (userPurchasedPrice != request.PurchasedPrice)
            {
                return BaseResponse<AddPaymentProofModelResponse>.Error("The purchased price does not match the selected pricing tier and billing cycle. Please review your selection and try again.");
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
                                                     .Include(u => u.User)
                                                     .Where(x => x.UserId == currentUserId)
                                                     .OrderByDescending(x => x.CreatedAt)
                                                     .FirstOrDefaultAsync(cancellationToken);

            if (latestPaymentProof?.Status == PaymentStatus.Pending)
            {
                return BaseResponse<AddPaymentProofModelResponse>.Error("Your previous payment proof is still under review. Please wait for approval before submitting a new one.\r\n");
            }

            using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var paymentVerification = new PaymentVerification()
                {
                    AccountLimit = pricingTier.UptoAccounts,
                    PurchasePrice = userPurchasedPrice,
                    ActualPrice = actualPurchasePrice,
                    BasePricePerMonth = basePricePerMonth,
                    SavingAmount = actualPurchasePrice - userPurchasedPrice,
                    BillingCycle = request.BillingCylce,
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

                await transaction.CommitAsync(cancellationToken);

                var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

                if (user !=null)
                {
                    var userName = user.Name;

                    var email = user.Email;

                    var phoneNumber = user.ContactNumber;

                    var billingCycle = request.BillingCylce.GetInfo().Name;


                    _ = _emailService.SendPaymentVerificationEmail(userName, email, phoneNumber, billingCycle, userPurchasedPrice);
                }
               
            }
            catch (Exception ex)
            {
                if (!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("Logs");
                }

                var fileName = $"Logs\\Add-payment-verification-failed-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt";
                File.WriteAllText(fileName, $"Message:{ex.Message}\n => {ex.InnerException} \n Trace: {ex.StackTrace}");

                await transaction.RollbackAsync(cancellationToken);
                return BaseResponse<AddPaymentProofModelResponse>.Error(
                 "Failed to submit payment proof. Please try again or contact support if the issue persists.");
            }


            return BaseResponse<AddPaymentProofModelResponse>.Success("Payment proof submitted successfully! We'll review and activate your subscription soon.", new AddPaymentProofModelResponse());

        }
    }
}
