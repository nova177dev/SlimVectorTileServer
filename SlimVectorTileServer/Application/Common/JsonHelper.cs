using System.Text.Json;
using System.Text.Json.Serialization;

namespace SlimVectorTileServer.Application.Common
{
    public class JsonHelper
    {
        private readonly JsonSerializerOptions _serializerOptions;
        public JsonHelper()
        {
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }
        public string SerializeObject(object obj)
        {
            return JsonSerializer.Serialize(obj, _serializerOptions);
        }

        public T? DeserializeJson<T>(string? json)
        {
            return json != null ? JsonSerializer.Deserialize<T>(json, _serializerOptions) : default;
        }

        public T? DeserializeJson<T>(JsonElement json)
        {
            return JsonSerializer.Deserialize<T>(json.ToString(), _serializerOptions);
        }
    }
}
