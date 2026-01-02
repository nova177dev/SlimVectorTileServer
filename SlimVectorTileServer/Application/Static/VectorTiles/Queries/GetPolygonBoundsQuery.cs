using MediatR;
using SlimVectorTileServer.Domain.Entities.Static.VectorTileServer;

namespace SlimVectorTileServer.Application.Static.VectorTiles.Queries
{
    public class GetPolygonBoundsQuery : IRequest<PolygonBounds?>
    {
        public int Id { get; }

        public GetPolygonBoundsQuery(int id)
        {
            Id = id;
        }
    }
}