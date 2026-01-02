using System.Text.Json.Serialization;

namespace SlimVectorTileServer.Domain.Entities.Static.VectorTileServer
{
    public class PolygonBounds
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("level")]
        public int Level { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("centerLng")]
        public double CenterLng { get; set; }

        [JsonPropertyName("centerLat")]
        public double CenterLat { get; set; }

        [JsonPropertyName("boundsWest")]
        public double BoundsWest { get; set; }

        [JsonPropertyName("boundsSouth")]
        public double BoundsSouth { get; set; }

        [JsonPropertyName("boundsEast")]
        public double BoundsEast { get; set; }

        [JsonPropertyName("boundsNorth")]
        public double BoundsNorth { get; set; }
    }
}