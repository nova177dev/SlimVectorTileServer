using Microsoft.Extensions.Options;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
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
        private readonly WKTReader _wktReader;

        public TilesService(
            IVectorTileRepository vectorTileRepository,
            IOptions<TileSettings> tileSettings,
            IWebHostEnvironment environment)
        {
            _vectorTileRepository = vectorTileRepository;
            _tileSettings = tileSettings.Value;
            _environment = environment;
            _wktReader = new WKTReader(new GeometryFactory(new PrecisionModel(PrecisionModels.Floating)));
        }

        public async Task<byte[]> CreateTileAsync(int zoom, int xTile, int yTile, string uuid, int cluster)
        {
            return await Task.FromResult(CreateTile(zoom, xTile, yTile, uuid, cluster));
        }

        public async Task<byte[]> CreatePolygonTileAsync(int zoom, int xTile, int yTile, string uuid)
        {
            return await Task.FromResult(CreatePolygonTile(zoom, xTile, yTile, uuid));
        }

        public byte[] CreateTile(int zoom, int xTile, int yTile, string uuid, int cluster)
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
                var pois = QueryDatabaseForTileData(xTile, yTile, zoom, uuid, cluster);
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

        public byte[] CreatePolygonTile(int zoom, int xTile, int yTile, string uuid)
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
                var polygons = QueryDatabaseForPolygonTileData(xTile, yTile, zoom, uuid);
                stepStopwatch.Stop();
                databaseQueryTime = stepStopwatch.ElapsedMilliseconds;

                // Feature processing
                stepStopwatch.Restart();
                if (polygons.Tables.Count > 0)
                {
                    ProcessPolygonTileFeatures(polygons, minX, maxX, minY, maxY, tile);
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
                    debugMessage.AppendLine($"Polygon Tile \"z:{zoom} x:{xTile} y:{yTile}\" total processing time: {totalTime}ms");
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
                    Debug.WriteLine($"Error creating polygon tile: {ex.Message}");
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

        private DataSet QueryDatabaseForTileData(int xTile, int yTile, int zoom, string uuid, int cluster)
        {
            return _vectorTileRepository.GetTileData(xTile, yTile, zoom, uuid, cluster);
        }

        private DataSet QueryDatabaseForPolygonTileData(int xTile, int yTile, int zoom, string uuid)
        {
            return _vectorTileRepository.GetPolygonTileData(xTile, yTile, zoom, uuid);
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

        private void ProcessPolygonTileFeatures(DataSet polygons, double minX, double maxX, double minY, double maxY, VectorTile tile)
        {
            var layer = new Layer { Name = _tileSettings.DefaultPolygonLayerName };
            var tileBounds = new Envelope(minX, maxX, minY, maxY);

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _tileSettings.MaxDegreeOfParallelism ?? Environment.ProcessorCount
            };

            Parallel.ForEach(polygons.Tables[0].AsEnumerable(), parallelOptions, row =>
            {
                if (row["geometry_wkt"] != DBNull.Value)
                {
                    try
                    {
                        string wkt = row["geometry_wkt"].ToString()!;
                        var geometry = _wktReader.Read(wkt);

                        // Handle antimeridian crossing - split geometry if needed
                        geometry = HandleAntimeridianCrossing(geometry);

                        if (geometry == null || geometry.IsEmpty)
                            return;

                        // Check if the geometry intersects with the tile bounds
                        if (geometry.EnvelopeInternal.Intersects(tileBounds))
                        {
                            var attributes = new AttributesTable();

                            // Add all non-geometry columns as attributes
                            foreach (DataColumn column in polygons.Tables[0].Columns)
                            {
                                if (column.ColumnName != "geometry_wkt" && row[column] != DBNull.Value)
                                {
                                    attributes.Add(column.ColumnName, row[column]);
                                }
                            }

                            var feature = new Feature(geometry, attributes);

                            lock (layer.Features)
                            {
                                layer.Features.Add(feature);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_environment.IsDevelopment())
                        {
                            Debug.WriteLine($"Error parsing polygon geometry: {ex.Message}");
                        }
                    }
                }
            });

            tile.Layers.Add(layer);
        }

        /// <summary>
        /// Handles geometries that cross the antimeridian (±180° longitude) by normalizing coordinates
        /// </summary>
        private Geometry? HandleAntimeridianCrossing(Geometry geometry)
        {
            if (geometry == null || geometry.IsEmpty)
                return geometry;

            var envelope = geometry.EnvelopeInternal;

            // Check if geometry likely crosses the antimeridian
            // A geometry crossing the antimeridian will have a very wide longitude span (> 180°)
            double longitudeSpan = envelope.MaxX - envelope.MinX;

            if (longitudeSpan <= 180)
            {
                // Normal geometry, no antimeridian crossing
                return geometry;
            }

            // Geometry crosses antimeridian - normalize by shifting coordinates
            // This handles the case where coordinates wrap from +180 to -180
            try
            {
                var factory = geometry.Factory;

                // For geometries crossing the antimeridian, we need to shift negative longitudes
                // to positive (add 360) to make the geometry continuous
                var normalizedGeometry = NormalizeGeometryCoordinates(geometry);

                // After normalization, clip to valid bounds if needed
                if (normalizedGeometry != null && normalizedGeometry.IsValid)
                {
                    return normalizedGeometry;
                }

                return geometry;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling antimeridian crossing: {ex.Message}");
                return geometry;
            }
        }

        /// <summary>
        /// Normalizes geometry coordinates by shifting negative longitudes for antimeridian-crossing geometries
        /// </summary>
        private Geometry NormalizeGeometryCoordinates(Geometry geometry)
        {
            var coordinates = geometry.Coordinates;
            var newCoordinates = new Coordinate[coordinates.Length];

            // Determine if we should shift negative or positive longitudes
            // Count coordinates on each side of the antimeridian
            int negativeCount = coordinates.Count(c => c.X < 0);
            int positiveCount = coordinates.Count(c => c.X >= 0);

            // Shift the minority side to be continuous with the majority
            bool shiftNegativeToPositive = negativeCount <= positiveCount;

            for (int i = 0; i < coordinates.Length; i++)
            {
                double x = coordinates[i].X;
                double y = coordinates[i].Y;

                if (shiftNegativeToPositive && x < 0)
                {
                    // Shift negative longitudes to positive (e.g., -170 becomes 190)
                    x += 360;
                }
                else if (!shiftNegativeToPositive && x > 0)
                {
                    // Shift positive longitudes to negative (e.g., 170 becomes -190)
                    x -= 360;
                }

                newCoordinates[i] = new Coordinate(x, y);
            }

            // Recreate the geometry with normalized coordinates
            var factory = geometry.Factory;

            if (geometry is Polygon polygon)
            {
                var shell = factory.CreateLinearRing(GetNormalizedRingCoordinates(polygon.Shell.Coordinates, shiftNegativeToPositive));
                var holes = polygon.Holes.Select(h =>
                    factory.CreateLinearRing(GetNormalizedRingCoordinates(h.Coordinates, shiftNegativeToPositive))).ToArray();
                return factory.CreatePolygon(shell, holes);
            }
            else if (geometry is MultiPolygon multiPolygon)
            {
                var polygons = new Polygon[multiPolygon.NumGeometries];
                for (int i = 0; i < multiPolygon.NumGeometries; i++)
                {
                    var poly = (Polygon)multiPolygon.GetGeometryN(i);
                    var shell = factory.CreateLinearRing(GetNormalizedRingCoordinates(poly.Shell.Coordinates, shiftNegativeToPositive));
                    var holes = poly.Holes.Select(h =>
                        factory.CreateLinearRing(GetNormalizedRingCoordinates(h.Coordinates, shiftNegativeToPositive))).ToArray();
                    polygons[i] = factory.CreatePolygon(shell, holes);
                }
                return factory.CreateMultiPolygon(polygons);
            }

            return geometry;
        }

        private Coordinate[] GetNormalizedRingCoordinates(Coordinate[] coordinates, bool shiftNegativeToPositive)
        {
            var newCoordinates = new Coordinate[coordinates.Length];

            for (int i = 0; i < coordinates.Length; i++)
            {
                double x = coordinates[i].X;
                double y = coordinates[i].Y;

                if (shiftNegativeToPositive && x < 0)
                {
                    x += 360;
                }
                else if (!shiftNegativeToPositive && x > 0)
                {
                    x -= 360;
                }

                newCoordinates[i] = new Coordinate(x, y);
            }

            return newCoordinates;
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