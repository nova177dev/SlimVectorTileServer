using MediatR;

namespace SlimVectorTileServer.Application.Static.VectorTiles.Queries
{
    public class GetPolygonVectorTileQuery : IRequest<byte[]>
    {
        public int Z { get; }
        public int X { get; }
        public int Y { get; }
        public string UUID { get; }

        public GetPolygonVectorTileQuery(int z, int x, int y, string uuid)
        {
            Z = z;
            X = x;
            Y = y;
            UUID = uuid;
        }
    }
}