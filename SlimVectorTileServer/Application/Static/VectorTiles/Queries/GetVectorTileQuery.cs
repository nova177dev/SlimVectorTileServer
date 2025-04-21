using MediatR;

namespace SlimVectorTileServer.Application.Static.VectorTiles.Queries
{
    public class GetVectorTileQuery : IRequest<byte[]>
    {
        public int Z { get; }
        public int X { get; }
        public int Y { get; }
        public string UUID { get; }
        public int Cluster { get; }

        public GetVectorTileQuery(int z, int x, int y, string uuid, int cluster)
        {
            Z = z;
            X = x;
            Y = y;
            UUID = uuid;
            Cluster = cluster;
        }
    }
}

