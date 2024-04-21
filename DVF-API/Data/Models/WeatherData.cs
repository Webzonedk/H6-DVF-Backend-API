using System.ComponentModel.DataAnnotations;

namespace DVF_API.Data.Models
{
    public class WeatherData
    {
        [Key]
        public int WeatherDataId { get; set; }
        public float TemperatureC { get; set; }
        public float WindSpeed { get; set; }
        public float WindDirection { get; set; }
        public float WindGust { get; set; }
        public float RelativeHumidity { get; set; }
        public float Rain { get; set; }
        public float GlobalTiltedIrRadiance { get; set; }
        public DateTime DateAndTime { get; set; }
        public int LocationId { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation property
        public virtual Location Location { get; set; }
    }
}
