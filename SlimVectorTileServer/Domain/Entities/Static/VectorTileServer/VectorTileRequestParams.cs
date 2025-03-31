using System.Text.Json;

namespace SlimVectorTileServer.Domain.Entities.Static
{
    public class VectorTileRequestParams
    {
        public JsonElement? Data { get; set; }
    }
}
