namespace SlimVectorTileServer.Infrastructure.Options
{
    public class AppSettings
    {
        public const string SectionName = "AppSettings";

        /// <summary>
        /// Maximum number of requests allowed per minute per IP address
        /// </summary>
        public int RateLimitPerMinute { get; set; } = 600;
    }
}