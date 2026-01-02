using MediatR;
using SlimVectorTileServer.Application.Common;
using SlimVectorTileServer.Domain.Entities.Static.VectorTileServer;
using SlimVectorTileServer.Domain.Repositories;

namespace SlimVectorTileServer.Application.Static.VectorTiles.Queries
{
    public class GetPolygonBoundsQueryHandler : IRequestHandler<GetPolygonBoundsQuery, PolygonBounds?>
    {
        private readonly IVectorTileRepository _vectorTileRepository;
        private readonly AppLogger _appLogger;

        public GetPolygonBoundsQueryHandler(
            IVectorTileRepository vectorTileRepository,
            AppLogger appLogger)
        {
            _vectorTileRepository = vectorTileRepository;
            _appLogger = appLogger;
        }

        public async Task<PolygonBounds?> Handle(GetPolygonBoundsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                return await Task.FromResult(_vectorTileRepository.GetPolygonBounds(request.Id));
            }
            catch (Exception ex)
            {
                _appLogger.LogError(ex);
                throw;
            }
        }
    }
}