using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Enums;
using FBMMultiMessenger.Contracts.Shared;
using FBMMultiMessenger.Data.Database.DbModels;
using FBMMultiMessenger.Data.DB;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

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

            var registeredLocalServer = await _dbContext.LocalServers.FirstOrDefaultAsync(ls => ls.UserId == currentUser.Id && ls.SystemUUID == request.SystemUUID
                                                                    &&
                                                                    ls.MotherboardSerial == request.MotherboardSerial, cancellationToken);

            if (registeredLocalServer is not null)
            {
                await OnLocalServerUpdated(registeredLocalServer, request);
                return BaseResponse<RegisterLocalServerModelResponse>.Success("Local Server is already registered.", new RegisterLocalServerModelResponse() { LocalServerId = registeredLocalServer.UniqueId });

            }

            var isParsed = Enum.TryParse<Roles>(currentUser.Role, out Roles currentUserRole);

            if (!isParsed)
            {
                return BaseResponse<RegisterLocalServerModelResponse>.Error("Your account permissions are not configured correctly. Please contact support.");
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
                IsSuperServer = currentUserRole == Roles.SuperServer,
                RegisteredAt = DateTime.UtcNow
            };

            await _dbContext.LocalServers.AddAsync(newLocalServer, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return BaseResponse<RegisterLocalServerModelResponse>.Success("Local Server registered successfully.", new RegisterLocalServerModelResponse() { LocalServerId = newLocalServer.UniqueId });
        }



        private async Task OnLocalServerUpdated(Data.Database.DbModels.LocalServer registeredLocalServer, RegisterLocalServerModelRequest request)
        {
            if (registeredLocalServer.MaxBrowserCapacity != request.MaxBrowserCapacity ||
                registeredLocalServer.TotalMemoryGB != request.TotalMemoryGB ||
                registeredLocalServer.TotalStorageGB != request.TotalStorageGB ||
                registeredLocalServer.GraphicsCards != request.GraphicsCards ||
                registeredLocalServer.LogicalProcessors != request.LogicalProcessors ||
                registeredLocalServer.HasSSD != request.HasSSD ||
                registeredLocalServer.OperatingSystem != request.OperatingSystem ||
                registeredLocalServer.ProcessorName != request.ProcessorName ||
                registeredLocalServer.CoreCount != request.CoreCount ||
                registeredLocalServer.MaxClockSpeedMHz != request.MaxClockSpeedMHz ||
                registeredLocalServer.CurrentClockSpeedMHz != request.CurrentClockSpeedMHz ||
                registeredLocalServer.CpuArchitecture != request.CpuArchitecture ||
                registeredLocalServer.CpuThreadCount != request.CpuThreadCount ||
                registeredLocalServer.Is64BitOS != request.Is64BitOS)
            {

                registeredLocalServer.MaxBrowserCapacity = request.MaxBrowserCapacity;
                registeredLocalServer.TotalMemoryGB = request.TotalMemoryGB;
                registeredLocalServer.TotalStorageGB = request.TotalStorageGB;
                registeredLocalServer.GraphicsCards = request.GraphicsCards;
                registeredLocalServer.LogicalProcessors = request.LogicalProcessors;
                registeredLocalServer.HasSSD = request.HasSSD;
                registeredLocalServer.OperatingSystem = request.OperatingSystem;
                registeredLocalServer.ProcessorName = request.ProcessorName;
                registeredLocalServer.CoreCount = request.CoreCount;
                registeredLocalServer.MaxClockSpeedMHz = request.MaxClockSpeedMHz;
                registeredLocalServer.CurrentClockSpeedMHz = request.CurrentClockSpeedMHz;
                registeredLocalServer.CpuArchitecture = request.CpuArchitecture;
                registeredLocalServer.CpuThreadCount = request.CpuThreadCount;
                registeredLocalServer.Is64BitOS = request.Is64BitOS;

                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
