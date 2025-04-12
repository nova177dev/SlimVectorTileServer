using SlimVectorTileServer.Domain.Entities.Static.VectorTileServer;
using SlimVectorTileServer.Domain.Entities.Common;
using MediatR;

namespace SlimVectorTileServer.Application.Static.VectorTiles.Commands
{
    public class CreateRequestParamsCommand(VectorTileRequestParams requestParams) : IRequest<ApiResponse>
    {
        public VectorTileRequestParams RequestParams { get; } = requestParams;
    }
}