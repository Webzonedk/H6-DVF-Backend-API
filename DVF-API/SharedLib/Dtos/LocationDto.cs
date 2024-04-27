using System.Text.Json.Serialization;

namespace DVF_API.SharedLib.Dtos
{
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
