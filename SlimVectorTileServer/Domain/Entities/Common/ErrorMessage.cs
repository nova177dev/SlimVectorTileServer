namespace SlimVectorTileServer.Domain.Entities.Common
{
    public class ErrorMessage
    {
        public string? TraceUuid { get; set; }
        public int ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public string? Details { get; set; }
    }
}
