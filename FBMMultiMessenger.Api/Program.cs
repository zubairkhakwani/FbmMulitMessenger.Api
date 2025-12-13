using FBMMultiMessenger.Buisness.Exntesions;
using FBMMultiMessenger.Buisness.Models;
using FBMMultiMessenger.Buisness.SignalR;
using Serilog;
using Serilog.Events;
using System.Reflection;

namespace FBMMultiMessenger.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                var logger = new LoggerConfiguration()
                 .MinimumLevel.Debug()
                 .CreateLogger();
                builder.Logging.ClearProviders();
                builder.Logging.AddSerilog();
                builder.Host.UseSerilog((ctx, lc) => lc
                    .WriteTo.Console(LogEventLevel.Error)
                    .WriteTo.File(path: "Logs\\debug-.txt", rollingInterval: RollingInterval.Day)
                    .WriteTo.File(path: "Logs\\info-.txt", restrictedToMinimumLevel: LogEventLevel.Information, rollingInterval: RollingInterval.Day)
                    .WriteTo.File(path: "Logs\\error-.txt", restrictedToMinimumLevel: LogEventLevel.Error, rollingInterval: RollingInterval.Day)
                    .WriteTo.File(path: "Logs\\fatal-.txt", restrictedToMinimumLevel: LogEventLevel.Fatal, rollingInterval: RollingInterval.Day)
                    .WriteTo.File("Logs\\log-.txt", rollingInterval: RollingInterval.Day));

                // Add services to the container.
                builder.Services.AddControllers();
                builder.Services.RegisterDatabase(builder.Configuration);
                builder.Services.AddSwagger();
                builder.Services.RegisterMediatR();
                builder.Services.RegisterAutoMapper(typeof(IServiceCollectionExtension).GetTypeInfo().Assembly, typeof(Program).GetTypeInfo().Assembly);
                builder.Services.AddTokenAuth(builder.Configuration);
               builder.Services.AddCors(builder.Configuration);
                builder.Services.RegisterServices();

                builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

                var app = builder.Build();

                if (!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("Logs");
                }

                try
                {
#if !DEBUG
                    using (var scope = app.Services.CreateScope())
                    {
                        //var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        //db.Database.Migrate();
                    }
#endif

                }
                catch (Exception ex)
                {
                    File.WriteAllText($"Logs\\db-migration-fail-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid()}.txt", $"Error while applying migration. exception is {ex.Message}, inner => {ex.InnerException?.Message}");
                }

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment() || true)
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseHttpsRedirection();

                app.UseAuthentication();
                app.UseAuthorization();
                app.UseCors("AllowedOrigins");
                app.UseStaticFiles();
                app.MapHub<ChatHub>("/chathub");
                app.MapControllers();
                app.Run();
            }
            catch (Exception ex)
            {
                if (!Directory.Exists("Logs"))
                {
                    Directory.CreateDirectory("Logs");
                }

                File.WriteAllText($"Logs\\crash-{DateTime.Now:yyyy MM dd hh mm ss}-{Guid.NewGuid()}.txt", $"Error while starting application: exception is {ex.Message}.\n inner => {ex.InnerException?.Message} \n Stack Strace : {ex.StackTrace} ");

                Log.Fatal(ex, "An error occurred while starting the application");
            }
        }
    }
}
