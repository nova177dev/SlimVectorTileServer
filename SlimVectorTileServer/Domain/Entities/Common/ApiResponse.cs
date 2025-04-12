using System.Text.Json;

namespace SlimVectorTileServer.Domain.Entities.Common
{
    public class ApiResponse
    {
        public required string TraceUuid { get; set; }
        public required int ResponseCode { get; set; }
        public required string ResponseMessage { get; set; }
        public JsonElement Data { get; set; }
    }
}
