using FBMMultiMessenger.Buisness.Helpers;
using FBMMultiMessenger.Buisness.Service;
using FBMMultiMessenger.Buisness.Service.Background;
using FBMMultiMessenger.Buisness.Service.IServices;
using FBMMultiMessenger.Buisness.SignalR;
using FBMMultiMessenger.Data.DB;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;

namespace FBMMultiMessenger.Buisness.Exntesions
{
    public static class IServiceCollectionExtension
    {
        public static IServiceCollection RegisterDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("DefaultConnection")!;
            services.AddDbContext<ApplicationDbContext>(opt => opt.UseNpgsql(connectionString));
            return services;
        }

        public static IServiceCollection RegisterMediatR(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IServiceCollectionExtension).GetTypeInfo().Assembly));
            return services;
        }

        public static IServiceCollection RegisterAutoMapper(this IServiceCollection services, params Assembly[] assemblies)
        {
            services.AddAutoMapper(assemblies);
            return services;
        }

        public static IServiceCollection AddSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description =
                        "JWT Authorization header using the Bearer scheme. \r\n\r\n " +
                        "Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\n" +
                        "Example: \"Bearer 12345abcdef\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                                        {
                                            Type = ReferenceType.SecurityScheme,
                                            Id = "Bearer"
                                        },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header
                        },
                        new List<string>()
                    }
                });
            });
            return services;
        }

        public static IServiceCollection AddTokenAuth(this IServiceCollection services, IConfiguration configuration)
        {
            string key = configuration.GetValue<string>("ApiSettings:Key")!;

            services.AddAuthentication(x =>
             {
                 x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                 x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

             }).AddJwtBearer(x =>
             {
                 x.RequireHttpsMetadata = false;
                 x.SaveToken = true;
                 x.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuerSigningKey = true,
                     IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)),
                     ValidateIssuer = false,
                     ValidateAudience = false
                 };
             });

            return services;
        }

        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.AddSignalR();
            services.AddScoped<OneSignalService>();
            services.AddScoped<ChatHub>();
            services.AddHttpContextAccessor();
            services.AddScoped<CurrentUserService>();
            services.AddScoped<AesEncryptionHelper>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IVerificationCodeService, VerificationCodeService>();
            services.AddScoped<IUserAccountService, UserAccountService>();
            services.AddScoped<ILocalServerService, LocalServerService>();
            services.AddScoped<ISubscriptionServerProviderService, SubscriptionServerProviderService>();
            services.AddHostedService<LocalServerHeartbeatMonitorService>();
            return services;
        }

        //TODO: Take suggestion from Zubair bhai and only allow that are required.
        public static IServiceCollection AddCors(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()!;

            services.AddCors(options =>
            {
                options.AddPolicy("AllowedOrigins", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            return services;
        }
    }
}
