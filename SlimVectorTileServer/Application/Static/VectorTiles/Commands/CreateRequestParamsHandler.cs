using MediatR;
using SlimVectorTileServer.Domain.Entities.Common;
using SlimVectorTileServer.Domain.Repositories;
using System.Text.Json;

namespace SlimVectorTileServer.Application.Static.VectorTiles.Commands
{
    public class CreateRequestParamsHandler (
        IVectorTileRepository vectorTileRepository
    ) : IRequestHandler<CreateRequestParamsCommand, ApiResponse>
    {
        private readonly IVectorTileRepository _vectorTileRepository = vectorTileRepository; // Automatically initialized

        public Task<ApiResponse> Handle(CreateRequestParamsCommand request, CancellationToken cancellationToken)
        {
            // Get the JSON data from the repository
            JsonElement jsonData = _vectorTileRepository.CreateRequestParams(request.RequestParams);

            // Create a new ApiResponse with the required properties
            var response = new ApiResponse
            {
                TraceUuid = Guid.NewGuid().ToString(),
                ResponseCode = StatusCodes.Status200OK,
                ResponseMessage = "Success",
                Data = jsonData
            };

            return Task.FromResult(response);
        }
    }
}