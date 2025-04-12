using SlimVectorTileServer.Application.Common;
using SlimVectorTileServer.Infrastructure.Options;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace SlimVectorTileServer.Application.Static.VectorTiles.Queries
{
    public class GetVectorTileQueryHandler : IRequestHandler<GetVectorTileQuery, byte[]>
    {
        private readonly TilesService _tilesService;
        private readonly AppLogger _appLogger;
        private readonly IDistributedCache _cache;
        private readonly CacheSettings _cacheSettings;
        private readonly IWebHostEnvironment _environment;

        public GetVectorTileQueryHandler(
            TilesService tilesService,
            AppLogger appLogger,
            IDistributedCache cache,
            IOptions<CacheSettings> cacheSettings,
            IWebHostEnvironment environment)
        {
            _tilesService = tilesService;
            _appLogger = appLogger;
            _cache = cache;
            _cacheSettings = cacheSettings.Value;
            _environment = environment;
        }

        public async Task<byte[]> Handle(GetVectorTileQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Measure cache hit processing time
                var stopwatch = Stopwatch.StartNew();

                // Cache processing
                string cacheKey = $"tile_{request.Z}_{request.X}_{request.Y}_{request.UUID}";

                if (request.Z <= _cacheSettings.MaxCacheZoomLevel)
                {
                    byte[]? cachedTileData = await _cache.GetAsync(cacheKey, cancellationToken);

                    if (cachedTileData != null)
                    {
                        stopwatch.Stop();

                        if (_environment.IsDevelopment())
                        {
                            var debugMessage = new System.Text.StringBuilder();
                            debugMessage.AppendLine($"Tile \"z:{request.Z} x:{request.X} y:{request.Y}\" cache hit. total processing time: {stopwatch.ElapsedMilliseconds}ms");
                            Debug.Write(debugMessage.ToString());
                        }

                        return cachedTileData;
                    }
                }
                else
                {
                    stopwatch.Stop();
                }

                byte[] tileData = await _tilesService.CreateTileAsync(request.Z, request.X, request.Y, request.UUID);

                TimeSpan expirationTime = _cacheSettings.DefaultSlidingExpiration;

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
                throw;
            }
        }
    }
}
