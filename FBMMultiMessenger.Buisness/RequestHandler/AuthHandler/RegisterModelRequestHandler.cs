using AutoMapper.Configuration.Annotations;
using FBMMultiMessenger.Buisness.Request.Auth;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FBMMultiMessenger.Buisness.RequestHandler.AuthHandler
{
    public class RegisterModelRequestHandler : IRequestHandler<RegisterModelRequest, BaseResponse<RegisterModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IEmailService _emailService;

        public RegisterModelRequestHandler(ApplicationDbContext dbContext,IEmailService emailService)
        {
            this._dbContext=dbContext;
            this._emailService=emailService;
        }
        public async Task<BaseResponse<RegisterModelResponse>> Handle(RegisterModelRequest request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users
                                       .FirstOrDefaultAsync(x => x.Email.ToLower() == request.Email.ToLower());

            if (user is not null)
            {
                return BaseResponse<RegisterModelResponse>.Error("Email already in use");
            }

            var newUser = new User()
            {
                Name = request.Name,
                Email = request.Email,
                ContactNumber = request.ContactNumber,
                Password = request.Password,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _dbContext.Users.AddAsync(newUser, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _emailService.SendWelcomeEmailAsync(newUser.Email, newUser.Name);

            return BaseResponse<RegisterModelResponse>.Success("Resgistered Successfully", new RegisterModelResponse());
        }
    }
}
