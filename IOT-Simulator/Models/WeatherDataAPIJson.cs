namespace IOT_Simulator.Models
{
    public class WeatherDataAPIJson
    {
        public string time { get; set; }
        public float temperature_2m { get; set; }
        public float relative_humidity_2m { get; set; }
        public float rain { get; set; }
        public float wind_speed { get; set; }
        public float wind_direction_10m { get; set; }
        public float wind_gusts_10m { get; set; }
        public float global_tilted_irradiance_instant { get; set; }


    }
}
