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
using System.Text;
using MTS.Api.Services;
using MTS.Services;
using PayOS;
using Serilog;
using Serilog.Events;

namespace MTS;
public static class Program
{
    public static WebApplication App { get; private set; }
    public static IDbContextFactory<AppDbContext> Db { get; private set; }
    public static CloudflareR2Service R2 =>
        App.Services.GetRequiredService<CloudflareR2Service>();
    private static PayOSClient payOSClient;
    public static async Task<AppDbContext> CreateDbAsync()
    {
        return await Db.CreateDbContextAsync();
    }
    public static PayOSClient GetPayOSClient()
    {
        return payOSClient;
    }
    public static async Task Main(string[] args)
    {
        Console.Title = "MTS Server";
        try
        {
            bool enableSwagger = false;
            Console.OutputEncoding = Encoding.UTF8;
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("Logs/mts-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
                .CreateLogger();
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: true);
            var webPort = builder.Configuration.GetValue("ServerPorts:WebPort", 9481);
            builder.WebHost.UseUrls($"http://*:{webPort}");
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            Log.Information("Database configured: {DataSource}", connectionString?.Split(';').FirstOrDefault());
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
                .AddControllers();

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
                    Description = "Nhap JWT token: Bearer {your_token}"
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
                options.AddPolicy("GameCors", policy =>
                {
                    policy
                        .WithOrigins("https://play.nghichthien.online","http://localhost:5173","https://ios.nghichthien.online")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            builder.Services.AddOptions<MTS.Services.Clans.ClanOptions>()
                .Bind(builder.Configuration.GetSection("Clan"))
                .Validate(x => x.IsValid(), "Invalid Clan settings")
                .ValidateOnStart();
            builder.Services.AddSingleton<MTS.Services.Clans.IClanWorldCatalog, MTS.Services.Clans.ClanWorldCatalog>();
            builder.Services.AddSingleton<MTS.Services.Clans.IClanPaymentGateway, MTS.Services.Clans.ClanPaymentGateway>();
            builder.Services.AddSingleton<MTS.Services.Clans.ClanPaymentService>();
            builder.Services.AddSingleton<MTS.Services.Clans.ClanService>();
            builder.Services.AddSingleton<MTS.Services.Clans.ClanRealtimeService>();
            builder.Services.AddHostedService<MTS.Services.Clans.ClanOutboxWorker>();
            builder.Services.AddHostedService<TcpHostedService>();
            builder.Services.AddHostedService<ServerCommandService>();
            builder.Services.AddHostedService<DailyService>();
            builder.Services.AddHostedService<ReloadShopService>();
            builder.Services.AddHostedService<ReloadDailyQuestService>();
            builder.Services.AddHostedService<ArenaRankService>();
            builder.Services.AddHostedService<TuViRankService>();
            builder.Services.AddHostedService<MonsterKillRankService>();
            builder.Services.AddHostedService<RechargeRankService>();
            builder.Services.AddHostedService<MoneyVipSpendingRankService>();
            builder.Services.AddHostedService<AuctionService>();
            builder.Services.AddHostedService<PaymentService>();
            builder.Services.AddHostedService<Startup>();
            var configuration = builder.Configuration;
            payOSClient = new PayOSClient(
                configuration["PayOS:ClientID"]!,
                configuration["PayOS:ApiKey"]!,
                configuration["PayOS:ChecksumKey"]!
            );
            builder.Services.AddSingleton(payOSClient);
            builder.Services.AddSingleton(new SnowflakeIdGenerator(1));
            builder.Services.Configure<CloudflareR2Options>(
                builder.Configuration.GetSection(CloudflareR2Options.SectionName));
            builder.Services.AddSingleton<CloudflareR2Service>();
            builder.Services.AddTransient<MTS.Services.Clans.ClanImageService>();
            builder.Services.AddScoped<JwtService>();
            App = builder.Build();
            Db = App.Services.GetRequiredService<IDbContextFactory<AppDbContext>>();
            GameConfig.Init();
            if (App.Environment.IsDevelopment() || enableSwagger)
            {
                App.UseSwagger();
                App.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MTS API");
                    c.RoutePrefix = string.Empty;
                });
            }

            App.UseCors("GameCors");
            App.UseAuthentication();
            App.UseAuthorization();
            App.MapControllers();
            Log.Information("MTS server starting on web port {WebPort}", webPort);
            App.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "MTS server terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
