using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.Payment;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.Payment
{
    internal class GetMyVerificationStatusModelRequestHandler : IRequestHandler<GetMyVerificationStatusModelRequest, BaseResponse<GetMyVerificationStatusModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;

        public GetMyVerificationStatusModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }
        public async Task<BaseResponse<GetMyVerificationStatusModelResponse>> Handle(GetMyVerificationStatusModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra safety check ==> user can never be null as this handler is authorized
            if (currentUser is null)
            {
                return BaseResponse<GetMyVerificationStatusModelResponse>.Error("Please login again to continue.");
            }

            var paymentVerifications = await _dbContext.PaymentVerifications
                                                       .Where(x => x.UserId == currentUser.Id)
                                                       .OrderByDescending(x => x.CreatedAt)
                                                       .FirstOrDefaultAsync(cancellationToken);

            if (paymentVerifications is null)
            {
                return BaseResponse<GetMyVerificationStatusModelResponse>.Success("No payment verification yet", new GetMyVerificationStatusModelResponse());
            }

            var response = new GetMyVerificationStatusModelResponse()
            {
                Id = paymentVerifications.Id,
                AccountsPurchased = paymentVerifications.AccountLimit,
                ActualPrice = paymentVerifications.ActualPrice,
                PurchasePrice = paymentVerifications.PurchasePrice,
                UploadedAt = paymentVerifications.CreatedAt,
                ApprovedAt = paymentVerifications.ApprovedAt,
                RejectedAt = paymentVerifications.RejectedAt,
                ReviewNote = paymentVerifications.ReviewNote,
                Status = paymentVerifications.Status,
            };

            response.Description = paymentVerifications.RejectionReason switch
            {
                PaymentRejectionReason.AMOUNT_LESS_THAN_REQUIRED => PaymentRejectionMessages.AMOUNT_LESS_THAN_REQUIRED,
                PaymentRejectionReason.AMOUNT_GREATER_THAN_REQUIRED => PaymentRejectionMessages.AMOUNT_GREATER_THAN_REQUIRED,
                PaymentRejectionReason.DUPLICATE_SUBMISSION => PaymentRejectionMessages.DUPLICATE_SUBMISSION,
                PaymentRejectionReason.SUSPECTED_FRAUD => PaymentRejectionMessages.SUSPECTED_FRAUD,
                PaymentRejectionReason.PROOF_NOT_VISIBLE => PaymentRejectionMessages.PROOF_NOT_VISIBLE,
                PaymentRejectionReason.CURRENCY_MISMATCH => PaymentRejectionMessages.CURRENCY_MISMATCH,
                PaymentRejectionReason.INCOMPLETE_INFORMATION => PaymentRejectionMessages.INCOMPLETE_INFORMATION,
                PaymentRejectionReason.PROOF_TAMPERED => PaymentRejectionMessages.PROOF_TAMPERED,
                PaymentRejectionReason.PAYMENT_DATE_MISMATCH => PaymentRejectionMessages.PAYMENT_DATE_MISMATCH,
                PaymentRejectionReason.RECIPIENT_ACCOUNT_INCORRECT => PaymentRejectionMessages.RECIPIENT_ACCOUNT_INCORRECT,
                PaymentRejectionReason.OTHER => PaymentRejectionMessages.OTHER,
                _ => "Your payment is currently under review. Our team will verify your submission shortly and you'll be notified once your subscription is activated.",
            };

            return BaseResponse<GetMyVerificationStatusModelResponse>.Success("Success", response);
        }
    }
}
