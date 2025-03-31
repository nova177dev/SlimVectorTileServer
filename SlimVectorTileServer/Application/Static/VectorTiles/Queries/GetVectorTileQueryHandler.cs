using SlimVectorTileServer.Application.Common;
using SlimVectorTileServer.Application.Static.Queries;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using System.Diagnostics;

namespace SlimVectorTileServer.Application.Express.Queries
{
    public class GetVectorTileQueryHandler : IRequestHandler<GetVectorTileQuery, byte[]>
    {
        private readonly TilesService _tilesService;
        private readonly AppLogger _appLogger;
        private readonly IDistributedCache _cache;

        public GetVectorTileQueryHandler(TilesService tilesService, AppLogger appLogger, IDistributedCache cache)
        {
            _tilesService = tilesService;
            _appLogger = appLogger;
            _cache = cache;
        }

        public async Task<byte[]> Handle(GetVectorTileQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Cache processing
                string cacheKey = $"tile_{request.Z}_{request.X}_{request.Y}_{request.UUID}";
                // Limit caching to zoom levels up to 10
                if (request.Z <= 10)
                {
                    byte[] cachedTileData = await _cache.GetAsync(cacheKey, cancellationToken);

                    if (cachedTileData != null)
                    {
                        Debug.WriteLine($"Cache hit:tile_{request.Z}_{request.X}_{request.Y}_{request.UUID}");
                        return cachedTileData;
                    }
                }

                byte[] tileData = await _tilesService.CreateTileAsync(request.Z, request.X, request.Y, request.UUID, cancellationToken);

                TimeSpan expirationTime = request.Z switch
                {
                    >= 0 and <= 3 => TimeSpan.FromDays(7),   // 1 week for zoom levels 0-3
                    >= 4 and <= 6 => TimeSpan.FromHours(72), // 72 hours for zoom levels 4-6
                    _ => TimeSpan.FromHours(24)              // 24 hours for all other levels
                };

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expirationTime
                };

                if (request.Z <= 10)
                {
                    await _cache.SetAsync(cacheKey, tileData, cacheOptions, cancellationToken);
                }

                return tileData;
            }
            catch (Exception ex)
            {
                _appLogger.LogError(ex);
                throw new Exception($"Tile generation failed");
            }
        }
    }
}
