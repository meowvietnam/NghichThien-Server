using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MTS.Data;
using MTS.Game;
using MTS.Network;
using Newtonsoft.Json.Converters;
using System;
using System.Text;
using MTS.Converter;
using MTS.Models;
using Newtonsoft.Json;

namespace MTS;

public static class Program
{
    public static WebApplication App { get; private set; }
    public static IDbContextFactory<AppDbContext> Db { get; private set; }

    public static async Task<AppDbContext> CreateDbAsync()
    {
        return await Db.CreateDbContextAsync();
    }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var webPort = builder.Configuration.GetValue("ServerPorts:WebPort", 9481);
        builder.WebHost.UseUrls($"http://*:{webPort}");
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Error);
        builder.Services.AddDbContextFactory<AppDbContext>(options =>
        {
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                sqlOptions =>
                {
                    sqlOptions.CommandTimeout(30);
                    sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                });
        });
        builder.Services
            .AddControllers()
            .AddNewtonsoftJson(options => { options.SerializerSettings.Converters.Add(new StringEnumConverter()); });

        var jwtKey = builder.Configuration["Jwt:Key"]
                     ?? throw new InvalidOperationException("Jwt:Key is not configured in appsettings.");
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        builder.Services.AddAuthorization();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "MTS REST API",
                Version = "v1",
                Description = "MTS REST API"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Nhập JWT token: Bearer {your_token}"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    []
                }
            });
        });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        });

        builder.Services.AddHostedService<TcpHostedService>();
        App = builder.Build();
        Db = App.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
        GameConfig.Init();
        if (App.Environment.IsDevelopment())
        {
            App.UseSwagger();
            App.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "MTS API");
                c.RoutePrefix = string.Empty;
            });
        }

        App.UseCors("AllowAll");
        App.UseAuthentication();
        App.UseHttpsRedirection();
        App.UseAuthorization();
        App.MapControllers();
        App.Run();
    }
}