using System;

namespace SlimVectorTileServer.Infrastructure.Options
{
    public class ConnectionStringsSettings
    {
        public const string SectionName = "ConnectionStrings";

        /// <summary>
        /// The main database connection string
        /// </summary>
        public string SlimVectorTileServer { get; set; } = string.Empty;

        /// <summary>
        /// The cache database connection string
        /// </summary>
        public string SlimVectorTileServerCache { get; set; } = string.Empty;
    }
}