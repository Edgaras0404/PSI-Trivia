using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TriviaBackend.Data;
using TriviaBackend.Hubs;
using TriviaBackend.Services;

namespace TriviaBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //log only message with time
            builder.Host.UseSerilog((context, loggerConfig) =>
                loggerConfig
                .MinimumLevel.Error()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] {Message:lj}{NewLine}")
                .WriteTo.File(
                    "logs/app.log",
                    outputTemplate: "[{Timestamp:HH:mm:ss}] {Message:lj}{NewLine}",
                    rollingInterval: RollingInterval.Day
                )
            );

            builder.Services.AddControllers();
            builder.Services.AddSignalR();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:3000", //front http
                        "https://localhost:3001" //front https
                        )
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<TriviaDbContext>(options =>
                options.UseNpgsql(connectionString));

            builder.Services.AddScoped<QuestionService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<PlayerService>();
            builder.Services.AddScoped<DBQuestionsService>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            var hubContext = app.Services.GetRequiredService<IHubContext<GameHub>>();
            GameHub.SetHubContext(hubContext);

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection(); // Enforce HTTPS redirection from HTTP (5000 to HTTPS 5001)

            app.UseCors("AllowAll");
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<GameHub>("/gamehub");

            app.Run();
        }
    }
}
