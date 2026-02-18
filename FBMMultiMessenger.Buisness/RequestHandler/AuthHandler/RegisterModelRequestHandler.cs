using FBMMultiMessenger.Buisness.Request.Auth;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.AuthHandler
{
    public class RegisterModelRequestHandler : IRequestHandler<RegisterModelRequest, BaseResponse<RegisterModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;

        public RegisterModelRequestHandler(ApplicationDbContext dbContext, IEmailService emailService)
        {
            this._dbContext=dbContext;
            this._emailService=emailService;
        }
        public async Task<BaseResponse<RegisterModelResponse>> Handle(RegisterModelRequest request, CancellationToken cancellationToken)
        {
            request.Email = request.Email.ToLowerInvariant();

            var user = await _dbContext.Users
                                       .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

            if (user is not null)
            {
                return BaseResponse<RegisterModelResponse>.Error("Email already in use");
            }

            var newUser = new User()
            {
                Name = request.Name,
                Email = request.Email.Trim().ToLower(),
                ContactNumber = request.ContactNumber,
                Password = request.Password,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                RoleId = (int)Roles.Customer
            };

            var trialConfig = await _dbContext.TrialConfigurations.FirstOrDefaultAsync(cancellationToken);
            var canAvailTrial = trialConfig is not null && trialConfig.IsEnabled;

            var trialDurationDays = trialConfig?.DurationDays ?? 0;
            var trialAccounts = trialConfig?.MaxAccounts ?? 0;

            if (canAvailTrial)
            {
                var trialSubscription = new Data.Database.DbModels.Subscription()
                {
                    IsActive = true,
                    IsTrial = true,
                    MaxLimit = trialAccounts,
                    StartedAt = DateTime.UtcNow,
                    ExpiredAt = DateTime.UtcNow.AddDays(trialDurationDays),
                };

                newUser.Subscriptions = new List<Data.Database.DbModels.Subscription> { trialSubscription };
            }

            newUser.HasAvailedTrial = canAvailTrial;

            await _dbContext.Users.AddAsync(newUser, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _ = _emailService.SendWelcomeEmailAsync(newUser.Email, newUser.Name, hasAvailedTrial: canAvailTrial, trialAccounts, trialDurationDays);

            var modelResponse = new RegisterModelResponse()
            {
                HasAvailedTrial = canAvailTrial,
                TrialAccounts = canAvailTrial ? trialAccounts : 0,
                TrialDays = canAvailTrial ? trialDurationDays : 0,
            };

            return BaseResponse<RegisterModelResponse>.Success("Resgistered Successfully", modelResponse);
        }
    }
}
