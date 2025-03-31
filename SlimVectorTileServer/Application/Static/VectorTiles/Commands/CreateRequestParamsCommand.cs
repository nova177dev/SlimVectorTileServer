using SlimVectorTileServer.Domain.Entities.Static;
using MediatR;

namespace SlimVectorTileServer.Application.Static.Commands
{
    public class CreateRequestParamsCommand : IRequest<VectorTileRequestParams>
    {
        public VectorTileRequestParams RequestParams { get; set; }
        public CreateRequestParamsCommand(VectorTileRequestParams requestParams)
        {
            RequestParams = requestParams;
        }
    }
}
