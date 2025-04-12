using DotNetEnv;
using SlimVectorTileServer.Application.Common;
using SlimVectorTileServer.Domain.Entities.Common;
using SlimVectorTileServer.Infrastructure.Data;
using SlimVectorTileServer.Infrastructure.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using System.Data;
using System.IO.Compression;
using System.Reflection;
using System.Threading.RateLimiting;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Load environment variables
    Env.Load();
    builder.Configuration.AddEnvironmentVariables();

    // Configure Serilog for file logging (json format)
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .WriteTo.Console()
        .WriteTo.File(new JsonFormatter(), "Logs/applog-.json", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    builder.Host.UseSerilog();

    // Register options
    builder.Services.Configure<AppSettings>(
        builder.Configuration.GetSection(AppSettings.SectionName));
    builder.Services.Configure<CorsSettings>(
        builder.Configuration.GetSection(CorsSettings.SectionName));
    builder.Services.Configure<CacheSettings>(
        builder.Configuration.GetSection(CacheSettings.SectionName));
    builder.Services.Configure<SwaggerSettings>(
        builder.Configuration.GetSection(SwaggerSettings.SectionName));
    builder.Services.Configure<TileSettings>(
        builder.Configuration.GetSection(TileSettings.SectionName));
    builder.Services.Configure<ConnectionStringsSettings>(
        builder.Configuration.GetSection(ConnectionStringsSettings.SectionName));

    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()));
    builder.Services.AddTransient<IDbConnection>(provider =>
        new SqlConnection(builder.Configuration.GetConnectionString("SlimVectorTileServer"))
    );

    // Add services to the container.
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<AppLogger>();
    builder.Services.AddScoped<AppDbDataContext>();
    builder.Services.AddScoped<ResponseHandler>();
    builder.Services.AddSingleton<JsonHelper>();
    builder.Services.AddScoped<TilesService>();

    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var appSettings = builder.Configuration.GetSection(AppSettings.SectionName).Get<AppSettings>();
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = appSettings?.RateLimitPerMinute ?? 600,
                    Window = TimeSpan.FromMinutes(1)
                });
        });
    });

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    });

    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errorMessage = new ErrorMessage
                {
                    TraceUuid = Guid.NewGuid().ToString(),
                    ResponseCode = StatusCodes.Status400BadRequest,
                    ResponseMessage = "Model validation failed"
                };

                Log.Error("Model validation failed: {ErrorMessage}", errorMessage.ResponseMessage);

                return new BadRequestObjectResult(errorMessage);
            };
        });

    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<GzipCompressionProvider>();
    });
    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Fastest;
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        var swaggerSettings = builder.Configuration.GetSection(SwaggerSettings.SectionName).Get<SwaggerSettings>();
        c.SwaggerDoc(swaggerSettings?.Version ?? "v1", new OpenApiInfo
        {
            Title = swaggerSettings?.Title ?? "Slim Vector Tile Server",
            Version = swaggerSettings?.Version ?? "v1",
            Description = swaggerSettings?.Description ?? "A lightweight, high-performance vector tile server built with .NET Core that dynamically generates vector tiles from MS Sql Server database data.",
            License = new OpenApiLicense {
                Name = swaggerSettings?.LicenseName ?? "MIT",
                Url = new Uri(swaggerSettings?.LicenseUrl ?? "https://github.com/nova177dev/SlimVectorTileServer/blob/master/LICENSE.txt")
            },
            Contact = new OpenApiContact
            {
                Name = swaggerSettings?.ContactName ?? "Anton V. Novoseltsev",
                Email = swaggerSettings?.ContactEmail ?? "nova177dev@gmail.com"
            }
        });
    });

    builder.Services.AddCors(options =>
    {
        var corsSettings = builder.Configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>();
        options.AddPolicy(corsSettings?.PolicyName ?? "AllowSpecificOrigins",
            builder =>
            {
                builder.WithOrigins(corsSettings?.AllowedOrigins?.ToArray() ?? new[] { "http://localhost:3000" });

                if (corsSettings?.AllowAnyMethod ?? true)
                {
                    builder.AllowAnyMethod();
                }

                if (corsSettings?.AllowAnyHeader ?? true)
                {
                    builder.AllowAnyHeader();
                }
            });
    });

    builder.Services.AddDistributedSqlServerCache(options =>
    {
        var cacheSettings = builder.Configuration.GetSection(CacheSettings.SectionName).Get<CacheSettings>();
        options.ConnectionString = builder.Configuration.GetConnectionString(
            cacheSettings?.ConnectionStringName ?? "SlimVectorTileServerCache");
        options.SchemaName = cacheSettings?.SchemaName ?? "dbo";
        options.TableName = cacheSettings?.TableName ?? "vector_tile_cache";
        options.DefaultSlidingExpiration = cacheSettings?.DefaultSlidingExpiration ?? TimeSpan.FromHours(24);
        options.ExpiredItemsDeletionInterval = cacheSettings?.ExpiredItemsDeletionInterval ?? TimeSpan.FromHours(72);
    });

    var app = builder.Build();

    app.UseCors("AllowSpecificOrigins");

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<ExceptionHandler>();
    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.UseStaticFiles(new StaticFileOptions
    {
        ServeUnknownFileTypes = true,
        DefaultContentType = "application/octet-stream",
        FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"))
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
public partial class Program { }
