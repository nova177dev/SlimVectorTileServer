using SlimVectorTileServer.Infrastructure.Data;
using SlimVectorTileServer.Infrastructure.Options;
using Microsoft.Extensions.Options;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO.VectorTiles;
using NetTopologySuite.IO.VectorTiles.Mapbox;
using System.Data;
using System.Diagnostics;
using System.IO.Compression;

namespace SlimVectorTileServer.Application.Common
{
    public class TilesService
    {
        private readonly AppDbDataContext _dbDataContext;
        private readonly TileSettings _tileSettings;

        public TilesService(
            AppDbDataContext dbDataContext,
            IOptions<TileSettings> tileSettings)
        {
            _dbDataContext = dbDataContext;
            _tileSettings = tileSettings.Value;
        }
        public async Task<byte[]> CreateTileAsync(int zoom, int xTile, int yTile, string uuid, CancellationToken cancellationToken)
        {
            return await Task.FromResult(CreateTile(zoom, xTile, yTile, uuid));
        }
        public byte[] CreateTile(int zoom, int xTile, int yTile, string uuid)
        {
            var tileDefinition = new NetTopologySuite.IO.VectorTiles.Tiles.Tile(xTile, yTile, zoom);
            var tile = new VectorTile { TileId = tileDefinition.Id };

            var factory = new GeometryFactory(new PrecisionModel(PrecisionModels.Floating));
            var layer = new Layer { Name = _tileSettings.DefaultLayerName };

            double tileSize = 1 << zoom;
            double minX = xTile / tileSize * 360 - 180;
            double maxX = (xTile + 1) / tileSize * 360 - 180;
            double minY = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (yTile + 1) / tileSize))) * 180 / Math.PI;
            double maxY = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * yTile / tileSize))) * 180 / Math.PI;

            try
            {
                var pois = _dbDataContext.RequestDbForDataSet(
                    _tileSettings.SchemaName,
                    _tileSettings.StoredProcedureName,
                    new
                    {
                        x = xTile,
                        y = yTile,
                        z = zoom,
                        uuid = uuid
                    }
                );

                if (pois.Tables.Count > 0)
                {
                    var stopwatch = Stopwatch.StartNew();
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

                            while (longitude < minX) longitude += 360;
                            while (longitude > maxX) longitude -= 360;

                            if (latitude >= minY && latitude <= maxY)
                            {
                                Point point = factory.CreatePoint(new Coordinate(longitude, latitude));

                                var feature = new Feature(point, new AttributesTable { });

                                lock (layer.Features)
                                {
                                    layer.Features.Add(feature);
                                }
                            }
                        }
                    });

                    stopwatch.Stop();
                    Debug.WriteLine($"Time taken for processing rows: {stopwatch.ElapsedMilliseconds} ms");
                }

                tile.Layers.Add(layer);

                byte[] mvtData;
                using (var stream = new MemoryStream())
                {
                    MapboxTileWriter.Write(tile, stream, MapboxTileWriter.DefaultMinLinealExtent, MapboxTileWriter.DefaultMinPolygonalExtent, _tileSettings.BufferSize);
                    mvtData = stream.ToArray();
                }

                byte[] compressedMvtData;
                using (var compressedStream = new MemoryStream())
                {
                    using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Compress))
                    {
                        gzipStream.Write(mvtData, 0, mvtData.Length);
                    }
                    compressedMvtData = compressedStream.ToArray();
                }

                return compressedMvtData;

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating tile: {ex.Message}");
                // Already re-throwing the original exception, which is good
                throw;
            }
        }
    }
}
