using System;
using System.Collections.Generic;

namespace SlimVectorTileServer.Infrastructure.Options
{
    public class CorsSettings
    {
        public const string SectionName = "CorsSettings";
        
        /// <summary>
        /// The policy name for CORS
        /// </summary>
        public string PolicyName { get; set; } = "AllowSpecificOrigins";
        
        /// <summary>
        /// List of allowed origins for CORS
        /// </summary>
        public List<string> AllowedOrigins { get; set; } = new List<string> { "http://localhost:3000" };
        
        /// <summary>
        /// Whether to allow any method
        /// </summary>
        public bool AllowAnyMethod { get; set; } = true;
        
        /// <summary>
        /// Whether to allow any header
        /// </summary>
        public bool AllowAnyHeader { get; set; } = true;
    }
}