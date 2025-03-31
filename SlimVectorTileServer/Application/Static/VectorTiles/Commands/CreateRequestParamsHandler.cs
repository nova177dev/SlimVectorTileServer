using SlimVectorTileServer.Application.Common;
using SlimVectorTileServer.Domain.Entities.Static;
using SlimVectorTileServer.Infrastructure.Data;
using MediatR;


namespace SlimVectorTileServer.Application.Static.Commands
{
    public class CreateRequestParamsHandler : IRequestHandler<CreateRequestParamsCommand, VectorTileRequestParams>
    {
        private readonly AppLogger _appLogger;
        private readonly AppDbDataContext _dbDataContext;
        private readonly JsonHelper _jsonHelper;

        public CreateRequestParamsHandler(AppLogger appLogger, AppDbDataContext dbDataContext, JsonHelper jsonHelper)
        {
            _appLogger = appLogger;
            _dbDataContext = dbDataContext;
            _jsonHelper = jsonHelper;
        }
        public Task<VectorTileRequestParams> Handle(CreateRequestParamsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                VectorTileRequestParams response = _jsonHelper.DeserializeJson<VectorTileRequestParams>(_dbDataContext.RequestDbForJson("dbo", "request_params_create", request.RequestParams)) ?? throw new ArgumentNullException(nameof(response), "Response Validation Failed");

                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _appLogger.LogError(ex);
                return Task.FromResult(new VectorTileRequestParams());
            }
        }
    }
}
