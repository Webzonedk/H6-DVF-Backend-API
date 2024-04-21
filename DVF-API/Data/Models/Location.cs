using System.ComponentModel.DataAnnotations;

namespace DVF_API.Data.Models
{
    public class Location
    {
        [Key]
        public int LocationId { get; set; }
        [Required]
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string StreetName { get; set; }
        public string StreetNumber { get; set; }
        public int CityId { get; set; }

        public virtual City City { get; set; }
        public virtual ICollection<WeatherData> WeatherDatas { get; set; } = new List<WeatherData>();
    }
}
