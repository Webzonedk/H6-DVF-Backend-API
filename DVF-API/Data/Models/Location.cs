using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DVF_API.Data.Models
{
    public class Location
    {

        [JsonPropertyName("Latitude")]
        public string Latitude { get; set; }
        [JsonPropertyName("Longitude")]
        public string Longitude { get; set; }
        [JsonPropertyName("StreetName")]
        public string StreetName { get; set; }
        [JsonPropertyName("HouseNumber")]
        public string StreetNumber { get; set; }
        public  City City { get; set; }

        
    }
}
