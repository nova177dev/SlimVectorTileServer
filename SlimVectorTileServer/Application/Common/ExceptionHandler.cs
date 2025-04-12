using DotNetEnv;
using SlimVectorTileServer.Domain.Entities.Common;
using System.Text.Json;

namespace SlimVectorTileServer.Application.Common
{
    public class ExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandler> _logger;
        private readonly IHostEnvironment _env;

        public ExceptionHandler(RequestDelegate next, ILogger<ExceptionHandler> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var errorResponse = new ErrorMessage
            {
                TraceUuid = Guid.NewGuid().ToString(),
                ResponseCode = StatusCodes.Status500InternalServerError,
                ResponseMessage = "An unexpected error occurred.",
                Details = _env.IsDevelopment() ? exception.Message : null
            };

            _logger.LogError(exception, exception.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }
    }
}
