namespace SlimVectorTileServer.Infrastructure.Options
{
    public class TileSettings
    {
        public const string SectionName = "TileSettings";

        /// <summary>
        /// The default layer name for vector tiles
        /// </summary>
        public string DefaultLayerName { get; set; } = "sites";

        /// <summary>
        /// The maximum degree of parallelism for tile generation
        /// Default: Use all available processors
        /// </summary>
        public int? MaxDegreeOfParallelism { get; set; } = null;

        /// <summary>
        /// The buffer size for tile generation
        /// </summary>
        public uint BufferSize { get; set; } = 4096;

        /// <summary>
        /// The schema name for the stored procedure
        /// </summary>
        public string SchemaName { get; set; } = "dbo";

        /// <summary>
        /// The stored procedure name for getting tile data
        /// </summary>
        public string StoredProcedureName { get; set; } = "sites_get";
    }
}