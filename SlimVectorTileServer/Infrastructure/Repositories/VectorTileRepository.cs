using System.Data;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SlimVectorTileServer.Domain.Entities.Static.VectorTileServer;
using SlimVectorTileServer.Domain.Repositories;
using SlimVectorTileServer.Infrastructure.Data;
using SlimVectorTileServer.Infrastructure.Options;

namespace SlimVectorTileServer.Infrastructure.Repositories
{
    public class VectorTileRepository : IVectorTileRepository
    {
        private readonly AppDbDataContext _dbDataContext;
        private readonly TileSettings _tileSettings;
        private readonly IWebHostEnvironment _environment;

        public VectorTileRepository(
            AppDbDataContext dbDataContext,
            IOptions<TileSettings> tileSettings,
            IWebHostEnvironment environment)
        {
            _dbDataContext = dbDataContext;
            _tileSettings = tileSettings.Value;
            _environment = environment;
        }

        public JsonElement CreateRequestParams(VectorTileRequestParams requestParams)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = _dbDataContext.RequestDbForJson("dbo", "request_params_create", requestParams);
            stopwatch.Stop();

            // Only log debug information in Development environment
            if (_environment.IsDevelopment())
            {
                // Use StringBuilder to create a single atomic message
                var debugMessage = new System.Text.StringBuilder();
                debugMessage.AppendLine($"DB operation time for CreateRequestParams: {stopwatch.ElapsedMilliseconds}ms");
                Debug.Write(debugMessage.ToString());
            }

            return result;
        }

        public DataSet GetTileData(int xTile, int yTile, int zoom, string uuid, int cluster)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = _dbDataContext.RequestDbForDataSet(
                _tileSettings.SchemaName,
                _tileSettings.PointsStoredProcedureName,
                new { x = xTile, y = yTile, z = zoom, uuid, cluster }
            );
            stopwatch.Stop();

            // Only log debug information in Development environment
            if (_environment.IsDevelopment())
            {
                var debugMessage = new System.Text.StringBuilder();
                debugMessage.AppendLine($"DB operation time for GetTileData z:{zoom} x:{xTile} y:{yTile} uuid:{uuid}: {stopwatch.ElapsedMilliseconds}ms");
                Debug.Write(debugMessage.ToString());
            }

            return result;
        }
    }
}