using SlimVectorTileServer.Infrastructure.Data;
using MediatR;
using SlimVectorTileServer.Domain.Entities.Common;
using System.Text.Json;

namespace SlimVectorTileServer.Application.Static.VectorTiles.Commands
{
    public class CreateRequestParamsHandler (
        AppDbDataContext dbDataContext
    ) : IRequestHandler<CreateRequestParamsCommand, ApiResponse>
    {
        private readonly AppDbDataContext _dbDataContext = dbDataContext; // Automatically initialized

        public Task<ApiResponse> Handle(CreateRequestParamsCommand request, CancellationToken cancellationToken)
        {
            // Get the JSON data from the database
            JsonElement jsonData = _dbDataContext.RequestDbForJson("dbo", "request_params_create", request.RequestParams);

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