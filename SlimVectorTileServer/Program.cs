using DotNetEnv;
using SlimVectorTileServer.Application.Common;
using SlimVectorTileServer.Domain.Entities.Common;
using SlimVectorTileServer.Infrastructure.Data;
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
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 600,
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
                    TraceUuid = context.HttpContext.TraceIdentifier,
                    ResponseCode = StatusCodes.Status400BadRequest,
                    ResponseMessage = string.Join("; ",
                        context.ModelState
                            .Where(ms => ms.Value != null && ms.Value.Errors.Count > 0)
                            .SelectMany(ms => ms.Value!.Errors.Select(e => e.ErrorMessage))
                            .ToList()
                    )
                };

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
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Slim Vector Tile Server",
            Version = "v1",
            Description = "A lightweight, high-performance vector tile server built with .NET Core that dynamically generates vector tiles from MS Sql Server database data.",
            License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://github.com/nova177dev/SlimVectorTileServer/blob/master/LICENSE.txt") },
            Contact = new OpenApiContact
            {
                Name = "Anton V. Novoseltsev",
                Email = "nova177dev@gmail.com"
            }
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSpecificOrigins",
            builder => builder
                .WithOrigins("http://localhost:3000")
                .AllowAnyMethod()
                .AllowAnyHeader());
    });

    builder.Services.AddDistributedSqlServerCache(options =>
    {
        options.ConnectionString = builder.Configuration.GetConnectionString(
            "SlimVectorTileServerCache");
        options.SchemaName = "dbo";
        options.TableName = "vector_tile_cache";
        options.DefaultSlidingExpiration = TimeSpan.FromHours(24);
        options.ExpiredItemsDeletionInterval = TimeSpan.FromHours(72);
    });

    var app = builder.Build();

    app.UseCors("AllowSpecificOrigins");

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<ExceptionHandler>();
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
