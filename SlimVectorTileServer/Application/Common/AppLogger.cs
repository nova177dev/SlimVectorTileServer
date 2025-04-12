namespace SlimVectorTileServer.Application.Common
{
    public class AppLogger(ILogger<AppLogger> logger) // Primary constructor
    {
        private readonly ILogger<AppLogger> _logger = logger;

        public void LogInformation(Exception ex, string message)
        {
            _logger.LogInformation(ex, "{Message}", message);
        }

        public void LogError(Exception ex)
        {
            _logger.LogError(ex, "An error occurred: {ErrorMessage}", ex.Message);
        }
    }
}