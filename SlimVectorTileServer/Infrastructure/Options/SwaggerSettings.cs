using System;
using Microsoft.OpenApi.Models;

namespace SlimVectorTileServer.Infrastructure.Options
{
    public class SwaggerSettings
    {
        public const string SectionName = "SwaggerSettings";
        
        /// <summary>
        /// The title of the API
        /// </summary>
        public string Title { get; set; } = "Slim Vector Tile Server";
        
        /// <summary>
        /// The version of the API
        /// </summary>
        public string Version { get; set; } = "v1";
        
        /// <summary>
        /// The description of the API
        /// </summary>
        public string Description { get; set; } = "A lightweight, high-performance vector tile server built with .NET Core that dynamically generates vector tiles from MS Sql Server database data.";
        
        /// <summary>
        /// The license name
        /// </summary>
        public string LicenseName { get; set; } = "MIT";
        
        /// <summary>
        /// The license URL
        /// </summary>
        public string LicenseUrl { get; set; } = "https://github.com/nova177dev/SlimVectorTileServer/blob/master/LICENSE.txt";
        
        /// <summary>
        /// The contact name
        /// </summary>
        public string ContactName { get; set; } = "Anton V. Novoseltsev";
        
        /// <summary>
        /// The contact email
        /// </summary>
        public string ContactEmail { get; set; } = "nova177dev@gmail.com";
    }
}