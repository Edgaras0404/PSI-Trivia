using Microsoft.EntityFrameworkCore;
using System;
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

            // Add services to the container
            builder.Services.AddControllers();
            builder.Services.AddSignalR();
            builder.Services.AddSingleton<QuestionService>();

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.WithOrigins("http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });
            // Get the connection string from appsettings.json
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            builder.Services.AddDbContext<TriviaDbContext>(options =>
    options.UseNpgsql(connectionString));

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAll");
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<GameHub>("/gamehub");

            app.Run();
        }
    }
}