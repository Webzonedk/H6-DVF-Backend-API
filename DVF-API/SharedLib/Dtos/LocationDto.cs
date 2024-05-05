using System.Text.Json.Serialization;

namespace DVF_API.SharedLib.Dtos
{
    /// <summary>
    /// This model is responsible for the location data transfer object
    /// </summary>
    public class LocationDto
    {
        [JsonPropertyName("Latitude")]
        public string? Latitude { get; set; }

        [JsonPropertyName("Longitude")]
        public string? Longitude { get; set; }

        [JsonPropertyName("StreetName")]
        public string? StreetName { get; set; }

        [JsonPropertyName("HouseNumber")]
        public string? StreetNumber { get; set; }

        [JsonPropertyName("PostalCode")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("City")]
        public string? CityName { get; set; }

    }
}
