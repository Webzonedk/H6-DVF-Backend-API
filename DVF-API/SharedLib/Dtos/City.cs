using System.Text.Json.Serialization;

namespace DVF_API.SharedLib.Dtos
{
    /// <summary>
    /// This model represents a city
    /// </summary>
    public class City
    {
        [JsonPropertyName("PostalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("City")]
        public string? CityName { get; set; }

    }
}
