using SlimVectorTileServer.Infrastructure.Data;
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

        public TilesService(AppDbDataContext dbDataContext)
        {
            _dbDataContext = dbDataContext;
        }
        public async Task<byte[]> CreateTileAsync(int z, int x, int y, string uuid, CancellationToken cancellationToken)
        {
            return await Task.FromResult(CreateTile(z, x, y, uuid));
        }
        public byte[] CreateTile(int z, int x, int y, string uuid)
        {
            var tileDefinition = new NetTopologySuite.IO.VectorTiles.Tiles.Tile(x, y, z);
            var tile = new VectorTile { TileId = tileDefinition.Id };

            var factory = new GeometryFactory(new PrecisionModel(PrecisionModels.Floating));
            var layer = new Layer { Name = "sites" };

            double tileSize = 1 << z;
            double minX = x / tileSize * 360 - 180;
            double maxX = (x + 1) / tileSize * 360 - 180;
            double minY = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * (y + 1) / tileSize))) * 180 / Math.PI;
            double maxY = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / tileSize))) * 180 / Math.PI;

            try
            {
                var pois = _dbDataContext.RequestDbForDataSet("dbo", "sites_get", new
                    {
                        x = x,
                        y = y,
                        z = z,
                        uuid = uuid
                    }
                );

                if (pois.Tables.Count > 0)
                {
                    var stopwatch = Stopwatch.StartNew();
                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount
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
                    uint bufferSize = 4096;
                    MapboxTileWriter.Write(tile, stream, MapboxTileWriter.DefaultMinLinealExtent, MapboxTileWriter.DefaultMinPolygonalExtent, bufferSize);
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
                throw;
            }
        }
    }
}
