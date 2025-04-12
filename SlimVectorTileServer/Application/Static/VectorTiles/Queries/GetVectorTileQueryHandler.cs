using SlimVectorTileServer.Application.Common;
using SlimVectorTileServer.Application.Static.VectorTiles.Queries;
using SlimVectorTileServer.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Linq;

namespace SlimVectorTileServer.Application.Static.VectorTiles.Queries
{
    public class GetVectorTileQueryHandler : IRequestHandler<GetVectorTileQuery, byte[]>
    {
        private readonly TilesService _tilesService;
        private readonly AppLogger _appLogger;
        private readonly IDistributedCache _cache;
        private readonly CacheSettings _cacheSettings;

        public GetVectorTileQueryHandler(
            TilesService tilesService,
            AppLogger appLogger,
            IDistributedCache cache,
            IOptions<CacheSettings> cacheSettings)
        {
            _tilesService = tilesService;
            _appLogger = appLogger;
            _cache = cache;
            _cacheSettings = cacheSettings.Value;
        }

        public async Task<byte[]> Handle(GetVectorTileQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Cache processing
                string cacheKey = $"tile_{request.Z}_{request.X}_{request.Y}_{request.UUID}";
                // Get max zoom level for caching from settings
                if (request.Z <= _cacheSettings.MaxCacheZoomLevel)
                {
                    byte[]? cachedTileData = await _cache.GetAsync(cacheKey, cancellationToken);

                    if (cachedTileData != null)
                    {
                        Debug.WriteLine($"Cache hit:tile_{request.Z}_{request.X}_{request.Y}_{request.UUID}");
                        return cachedTileData;
                    }
                }

                byte[] tileData = await _tilesService.CreateTileAsync(request.Z, request.X, request.Y, request.UUID, cancellationToken);

                // Determine cache expiration time based on zoom level using configuration
                TimeSpan expirationTime = _cacheSettings.DefaultSlidingExpiration;
                
                // Find matching zoom level expiration setting
                var expirationSetting = _cacheSettings.ZoomLevelExpirations
                    ?.FirstOrDefault(e => request.Z >= e.MinZoom && request.Z <= e.MaxZoom);
                
                if (expirationSetting != null)
                {
                    expirationTime = TimeSpan.FromHours(expirationSetting.ExpirationHours);
                }

                var cacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expirationTime
                };

                if (request.Z <= _cacheSettings.MaxCacheZoomLevel)
                {
                    await _cache.SetAsync(cacheKey, tileData, cacheOptions, cancellationToken);
                }

                return tileData;
            }
            catch (Exception ex)
            {
                _appLogger.LogError(ex);
                // Preserve the original exception by re-throwing it
                throw;
            }
        }
    }
}
