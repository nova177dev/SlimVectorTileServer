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

        public DataSet GetPolygonTileData(int xTile, int yTile, int zoom, string uuid)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = _dbDataContext.RequestDbForDataSet(
                _tileSettings.SchemaName,
                _tileSettings.PolygonsStoredProcedureName,
                new { x = xTile, y = yTile, z = zoom, uuid }
            );
            stopwatch.Stop();

            // Only log debug information in Development environment
            if (_environment.IsDevelopment())
            {
                var debugMessage = new System.Text.StringBuilder();
                debugMessage.AppendLine($"DB operation time for GetPolygonTileData z:{zoom} x:{xTile} y:{yTile} uuid:{uuid}: {stopwatch.ElapsedMilliseconds}ms");
                Debug.Write(debugMessage.ToString());
            }

            return result;
        }

        public PolygonBounds? GetPolygonBounds(int id)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = _dbDataContext.RequestDbForDataSet(
                    _tileSettings.SchemaName,
                    "polygon_bounds_get",
                    new { id }
                );
                stopwatch.Stop();

                if (_environment.IsDevelopment())
                {
                    var debugMessage = new System.Text.StringBuilder();
                    debugMessage.AppendLine($"DB operation time for GetPolygonBounds id:{id}: {stopwatch.ElapsedMilliseconds}ms");
                    debugMessage.AppendLine($"   - Tables count: {result.Tables.Count}");
                    if (result.Tables.Count > 0)
                    {
                        debugMessage.AppendLine($"   - Rows count: {result.Tables[0].Rows.Count}");
                    }
                    Debug.Write(debugMessage.ToString());
                }

                if (result.Tables.Count == 0 || result.Tables[0].Rows.Count == 0)
                {
                    return null;
                }

                var row = result.Tables[0].Rows[0];

                if (_environment.IsDevelopment())
                {
                    var debugMessage = new System.Text.StringBuilder();
                    debugMessage.AppendLine($"GetPolygonBounds row data for id:{id}:");
                    foreach (DataColumn col in result.Tables[0].Columns)
                    {
                        debugMessage.AppendLine($"   - {col.ColumnName}: {row[col]}");
                    }
                    Debug.Write(debugMessage.ToString());
                }

                return new PolygonBounds
                {
                    Id = Convert.ToInt32(row["id"]),
                    Name = row["name"]?.ToString() ?? string.Empty,
                    Level = Convert.ToInt32(row["level"]),
                    Type = row["type"]?.ToString() ?? string.Empty,
                    CenterLng = Convert.ToDouble(row["center_lng"]),
                    CenterLat = Convert.ToDouble(row["center_lat"]),
                    BoundsWest = Convert.ToDouble(row["bounds_west"]),
                    BoundsSouth = Convert.ToDouble(row["bounds_south"]),
                    BoundsEast = Convert.ToDouble(row["bounds_east"]),
                    BoundsNorth = Convert.ToDouble(row["bounds_north"])
                };
            }
            catch (Exception ex)
            {
                if (_environment.IsDevelopment())
                {
                    Debug.WriteLine($"Error in GetPolygonBounds id:{id}: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                throw;
            }
        }
    }
}