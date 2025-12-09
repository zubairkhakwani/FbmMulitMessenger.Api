using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FBMMultiMessenger.Buisness.RequestHandler.LocalServer
{
    internal class RegisterLocalServerModelRequestHandler : IRequestHandler<RegisterLocalServerModelRequest, BaseResponse<RegisterLocalServerModelResponse>>
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CurrentUserService _currentUserService;
        public RegisterLocalServerModelRequestHandler(ApplicationDbContext dbContext, CurrentUserService currentUserService)
        {
            this._dbContext=dbContext;
            this._currentUserService=currentUserService;
        }

        public async Task<BaseResponse<RegisterLocalServerModelResponse>> Handle(RegisterLocalServerModelRequest request, CancellationToken cancellationToken)
        {
            var currentUser = _currentUserService.GetCurrentUser();

            //Extra validation to ensure user is valid
            if (currentUser is null)
            {
                return BaseResponse<RegisterLocalServerModelResponse>.Error("Invalid Request, please login again to continue.");

            }

            var registeredLocalServer = await _dbContext.LocalServers.FirstOrDefaultAsync(ls => ls.SystemUUID == request.SystemUUID
                                                                    ||
                                                                    ls.MotherboardSerial == request.MotherboardSerial
                                                                    ||
                                                                    ls.ProcessorId == request.ProcessorId, cancellationToken);

            if (registeredLocalServer is not null)
            {
                return BaseResponse<RegisterLocalServerModelResponse>.Success("Local Server is already registered.", new RegisterLocalServerModelResponse() { LocalServerId = registeredLocalServer.UniqueId });

            }

            var newLocalServer = new Data.Database.DbModels.LocalServer
            {
                UniqueId = LocalServerHelper.GenereatetUniqueId(request.SystemUUID),
                UserId = currentUser.Id,
                TotalMemoryGB = request.TotalMemoryGB,
                LogicalProcessors = request.LogicalProcessors,
                ProcessorName = request.ProcessorName,
                CoreCount = request.CoreCount,
                MaxClockSpeedMHz = request.MaxClockSpeedMHz,
                CurrentClockSpeedMHz = request.CurrentClockSpeedMHz,
                CpuArchitecture = request.CpuArchitecture,
                CpuThreadCount = request.CpuThreadCount,
                GraphicsCards = request.GraphicsCards,
                TotalStorageGB = request.TotalStorageGB,
                HasSSD = request.HasSSD,
                OperatingSystem = request.OperatingSystem,
                Is64BitOS = request.Is64BitOS,
                SystemUUID = request.SystemUUID,
                MotherboardSerial = request.MotherboardSerial,
                ProcessorId = request.ProcessorId,
                MaxBrowserCapacity = request.MaxBrowserCapacity,
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            };

            await _dbContext.LocalServers.AddAsync(newLocalServer, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);


            return BaseResponse<RegisterLocalServerModelResponse>.Success("Local Server registered successfully.", new RegisterLocalServerModelResponse() { LocalServerId = newLocalServer.UniqueId });

        }
    }
}
