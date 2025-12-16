using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Request.LocalServer;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Contracts.Enums;
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
            var logMessages = new List<string>();
            logMessages.Add($"Register local server attempt");
            logMessages.Add($"Incoming request System UUId: {request.SystemUUID}");
            logMessages.Add($"Incoming request Motherboard: {request.MotherboardSerial}");
            logMessages.Add($"Incoming request Processor Id: {request.ProcessorId}");

            var currentUser = _currentUserService.GetCurrentUser();

            //Extra validation to ensure user is valid
            if (currentUser is null)
            {
                return BaseResponse<RegisterLocalServerModelResponse>.Error("Invalid Request, please login again to continue.");

            }

            var registeredLocalServer = await _dbContext.LocalServers.FirstOrDefaultAsync(ls => ls.SystemUUID == request.SystemUUID
                                                                    ||
                                                                    ls.MotherboardSerial == request.MotherboardSerial, cancellationToken);


            logMessages.Add($"Local Server already exist: {registeredLocalServer is not null}");
            if (registeredLocalServer is not null)
            {
                logMessages.Add($"Returning back the exisiting Unique id of this local server: {registeredLocalServer.UniqueId}");
                WriteLog(logMessages, "Local Server Already Regisrered");
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

            logMessages.Add($"New local Server  registered with the unique id: {newLocalServer.UniqueId}");
            WriteLog(logMessages, "New Local Server Regisrered");
            return BaseResponse<RegisterLocalServerModelResponse>.Success("Local Server registered successfully.", new RegisterLocalServerModelResponse() { LocalServerId = newLocalServer.UniqueId });

        }

        private void WriteLog(List<string> messages, string status)
        {
            try
            {
                if (!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("Logs");
                }

                var fileName = $"Logs\\{status}-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt";
                File.WriteAllText(fileName, string.Join(Environment.NewLine, messages));
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
}
