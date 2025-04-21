using System.Data;
using System.Text.Json;
using SlimVectorTileServer.Domain.Entities.Static.VectorTileServer;

namespace SlimVectorTileServer.Domain.Repositories
{
    public interface IVectorTileRepository
    {
        /// <summary>
        /// Retrieves JSON data from the database for creating request parameters
        /// </summary>
        /// <param name="requestParams">The request parameters</param>
        /// <returns>JSON data from the database</returns>
        JsonElement CreateRequestParams(VectorTileRequestParams requestParams);

        /// <summary>
        /// Retrieves points tile data from the database
        /// </summary>
        /// <param name="xTile">X coordinate of the tile</param>
        /// <param name="yTile">Y coordinate of the tile</param>
        /// <param name="zoom">Zoom level</param>
        /// <param name="uuid">UUID of the tile request</param>
        /// <returns>Dataset containing tile data</returns>
        DataSet GetTileData(int xTile, int yTile, int zoom, string uuid, int cluster);
    }
}