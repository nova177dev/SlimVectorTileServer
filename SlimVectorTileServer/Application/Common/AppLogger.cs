namespace SlimVectorTileServer.Application.Common
{
    public class AppLogger
    {
        private readonly ILogger<AppLogger> _logger;

        public AppLogger(ILogger<AppLogger> logger)
        {
            _logger = logger;
        }

        public void LogInformation(Exception ex, string message)
        {
            _logger.LogInformation(ex, message);
        }

        public void LogError(Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
}

