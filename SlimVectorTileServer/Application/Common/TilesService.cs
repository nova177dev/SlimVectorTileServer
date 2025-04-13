using Microsoft.Extensions.Options;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.VectorTiles.Mapbox;
using NetTopologySuite.IO.VectorTiles;
using SlimVectorTileServer.Domain.Repositories;
using SlimVectorTileServer.Infrastructure.Options;
using System.Data;
using System.Diagnostics;
using System.IO.Compression;

namespace SlimVectorTileServer.Application.Common
{
    public class TilesService
    {
        private readonly IVectorTileRepository _vectorTileRepository;
        private readonly TileSettings _tileSettings;
        private readonly IWebHostEnvironment _environment;

        public TilesService(
            IVectorTileRepository vectorTileRepository,
            IOptions<TileSettings> tileSettings,
            IWebHostEnvironment environment)
        {
            _vectorTileRepository = vectorTileRepository;
            _tileSettings = tileSettings.Value;
            _environment = environment;
        }

        public async Task<byte[]> CreateTileAsync(int zoom, int xTile, int yTile, string uuid)
        {
            return await Task.FromResult(CreateTile(zoom, xTile, yTile, uuid));
        }

        public byte[] CreateTile(int zoom, int xTile, int yTile, string uuid)
        {
            var totalStopwatch = Stopwatch.StartNew();
            var stepStopwatch = new Stopwatch();

            long initializationTime;
            long boundaryCalculationTime;
            long databaseQueryTime;
            long featureProcessingTime;
            long compressionTime;

            // Initialization
            stepStopwatch.Start();
            var tileDefinition = new NetTopologySuite.IO.VectorTiles.Tiles.Tile(xTile, yTile, zoom);
            var tile = new VectorTile { TileId = tileDefinition.Id };
            stepStopwatch.Stop();
            initializationTime = stepStopwatch.ElapsedMilliseconds;

            // Boundary calculation
            stepStopwatch.Restart();
            var (minX, maxX, minY, maxY) = CalculateTileBoundaries(zoom, xTile, yTile);
            stepStopwatch.Stop();
            boundaryCalculationTime = stepStopwatch.ElapsedMilliseconds;

            try
            {
                // Database query
                stepStopwatch.Restart();
                var pois = QueryDatabaseForTileData(xTile, yTile, zoom, uuid);
                stepStopwatch.Stop();
                databaseQueryTime = stepStopwatch.ElapsedMilliseconds;

                // Feature processing
                stepStopwatch.Restart();
                if (pois.Tables.Count > 0)
                {
                    ProcessTileFeatures(pois, minX, maxX, minY, maxY, tile);
                    stepStopwatch.Stop();
                }
                featureProcessingTime = stepStopwatch.ElapsedMilliseconds;

                // Tile compression
                stepStopwatch.Restart();
                var result = CompressTile(tile);
                stepStopwatch.Stop();
                compressionTime = stepStopwatch.ElapsedMilliseconds;

                // Total time
                totalStopwatch.Stop();
                long totalTime = totalStopwatch.ElapsedMilliseconds;

            if (_environment.IsDevelopment())
            {
                var debugMessage = new System.Text.StringBuilder();
                debugMessage.AppendLine($"Tile \"z:{zoom} x:{xTile} y:{yTile}\" total processing time: {totalTime}ms");
                debugMessage.AppendLine($"   - database query: {databaseQueryTime}ms");
                debugMessage.AppendLine($"   - initialization: {initializationTime}ms");
                debugMessage.AppendLine($"   - boundary calculation: {boundaryCalculationTime}ms");
                debugMessage.AppendLine($"   - feature processing: {featureProcessingTime}ms");
                debugMessage.AppendLine($"   - tile compression: {compressionTime}ms");

                Debug.Write(debugMessage.ToString());
            }

                return result;
            }
            catch (Exception ex)
            {
                if (_environment.IsDevelopment())
                {
                    Debug.WriteLine($"Error creating tile: {ex.Message}");
                }
                throw;
            }
        }

        private static (double minX, double maxX, double minY, double maxY) CalculateTileBoundaries(int zoom, int xTile, int yTile)
        {
            double tileSize = 1 << zoom;
            double minX = xTile / tileSize * 360 - 180;
            double maxX = (xTile + 1) / tileSize * 360 - 180;
            double minY = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (yTile + 1) / tileSize))) * 180 / Math.PI;
            double maxY = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * yTile / tileSize))) * 180 / Math.PI;

            return (minX, maxX, minY, maxY);
        }

        private DataSet QueryDatabaseForTileData(int xTile, int yTile, int zoom, string uuid)
        {
            return _vectorTileRepository.GetTileData(xTile, yTile, zoom, uuid);
        }

        private void ProcessTileFeatures(DataSet pois, double minX, double maxX, double minY, double maxY, VectorTile tile)
        {
            var factory = new GeometryFactory(new PrecisionModel(PrecisionModels.Floating));
            var layer = new Layer { Name = _tileSettings.DefaultLayerName };

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _tileSettings.MaxDegreeOfParallelism ?? Environment.ProcessorCount
            };

            Parallel.ForEach(pois.Tables[0].AsEnumerable(), parallelOptions, row =>
            {
                if (row["geo_lon"] != DBNull.Value && row["geo_lat"] != DBNull.Value)
                {
                    double longitude = Convert.ToDouble(row["geo_lon"]);
                    double latitude = Convert.ToDouble(row["geo_lat"]);
                    int count = Convert.ToInt32(row["count"]);

                    while (longitude < minX) longitude += 360;
                    while (longitude > maxX) longitude -= 360;

                    if (latitude >= minY && latitude <= maxY)
                    {
                        Point point = factory.CreatePoint(new Coordinate(longitude, latitude));
                        var feature = new Feature(point, new AttributesTable { { "count", count } });

                        lock (layer.Features)
                        {
                            layer.Features.Add(feature);
                        }
                    }
                }
            });

            tile.Layers.Add(layer);
        }

        private byte[] CompressTile(VectorTile tile)
        {
            byte[] mvtData;
            using (var stream = new MemoryStream())
            {
                MapboxTileWriter.Write(tile, stream, MapboxTileWriter.DefaultMinLinealExtent, MapboxTileWriter.DefaultMinPolygonalExtent, _tileSettings.BufferSize);
                mvtData = stream.ToArray();
            }

            using var compressedStream = new MemoryStream();
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                gzipStream.Write(mvtData, 0, mvtData.Length);
            }
            return compressedStream.ToArray();
        }
    }
}
