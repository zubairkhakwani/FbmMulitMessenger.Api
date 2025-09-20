using FBMMultiMessenger.Buisness.Exntesions;
using System.Reflection;
namespace FBMMultiMessengerServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            builder.Services.RegisterDatabase(builder.Configuration);
            builder.Services.AddSwagger();
            builder.Services.RegisterMediatR();
            builder.Services.RegisterAutoMapper(typeof(IServiceCollectionExtension).GetTypeInfo().Assembly, typeof(Program).GetTypeInfo().Assembly);
            builder.Services.AddTokenAuth(builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
