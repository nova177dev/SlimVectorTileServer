using System;
using System.Collections.Generic;

namespace SlimVectorTileServer.Infrastructure.Options
{
    /// <summary>
    /// Settings for cache expiration based on zoom level
    /// </summary>
    public class CacheExpirationSetting
    {
        /// <summary>
        /// Minimum zoom level
        /// </summary>
        public int MinZoom { get; set; }

        /// <summary>
        /// Maximum zoom level
        /// </summary>
        public int MaxZoom { get; set; }

        /// <summary>
        /// Expiration time in hours
        /// </summary>
        public int ExpirationHours { get; set; }
    }

    public class CacheSettings
    {
        public const string SectionName = "CacheSettings";

        /// <summary>
        /// The connection string name for the cache database
        /// </summary>
        public string ConnectionStringName { get; set; } = "SlimVectorTileServerCache";

        /// <summary>
        /// The schema name for the cache table
        /// </summary>
        public string SchemaName { get; set; } = "dbo";

        /// <summary>
        /// The table name for the cache
        /// </summary>
        public string TableName { get; set; } = "vector_tile_cache";

        /// <summary>
        /// The default sliding expiration time for cache entries
        /// </summary>
        public TimeSpan DefaultSlidingExpiration { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// The interval at which expired items are deleted from the cache
        /// </summary>
        public TimeSpan ExpiredItemsDeletionInterval { get; set; } = TimeSpan.FromHours(72);

        /// <summary>
        /// The maximum zoom level for which tiles will be cached
        /// </summary>
        public int MaxCacheZoomLevel { get; set; } = 10;

        /// <summary>
        /// Cache expiration settings for different zoom levels
        /// </summary>
        public List<CacheExpirationSetting> ZoomLevelExpirations { get; set; } = new List<CacheExpirationSetting>
        {
            new CacheExpirationSetting { MinZoom = 0, MaxZoom = 3, ExpirationHours = 168 }, // 7 days
            new CacheExpirationSetting { MinZoom = 4, MaxZoom = 6, ExpirationHours = 72 },  // 3 days
            // Default for other levels is DefaultSlidingExpiration
        };
    }
}