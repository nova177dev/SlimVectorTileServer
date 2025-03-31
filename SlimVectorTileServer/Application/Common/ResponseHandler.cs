using SlimVectorTileServer.Domain.Entities.Common;
using Microsoft.AspNetCore.Mvc;

namespace SlimVectorTileServer.Application.Common
{
    public class ResponseHandler
    {
        public ActionResult<T> HandleResponse<T>(T entity) where T : class
        {
            var responseCodeProperty = typeof(T).GetProperty("ResponseCode");
            var traceUuidProperty = typeof(T).GetProperty("TraceUuid");
            var responseMessageProperty = typeof(T).GetProperty("ResponseMessage");

            if (responseCodeProperty == null || traceUuidProperty == null || responseMessageProperty == null)
            {
                return new BadRequestObjectResult(new ErrorMessage { ResponseCode = StatusCodes.Status400BadRequest, ResponseMessage = "Invalid response entity" });
            }

            int responseCode = (int)(responseCodeProperty.GetValue(entity) ?? StatusCodes.Status400BadRequest);
            string traceUuid = (string)(traceUuidProperty.GetValue(entity) ?? string.Empty);
            string responseMessage = (string)(responseMessageProperty.GetValue(entity) ?? "Bad Request");

            return responseCode switch
            {
                StatusCodes.Status200OK => new OkObjectResult(entity),
                StatusCodes.Status201Created => new CreatedResult("", entity),
                StatusCodes.Status401Unauthorized => new UnauthorizedObjectResult(new ErrorMessage { TraceUuid = traceUuid, ResponseCode = StatusCodes.Status401Unauthorized, ResponseMessage = "Unauthorized" }),
                StatusCodes.Status404NotFound => new NotFoundObjectResult(new ErrorMessage { TraceUuid = traceUuid, ResponseCode = StatusCodes.Status404NotFound, ResponseMessage = "Not Found" }),
                _ => new BadRequestObjectResult(new ErrorMessage { TraceUuid = traceUuid, ResponseCode = StatusCodes.Status400BadRequest, ResponseMessage = responseMessage })
            };
        }
    }
}