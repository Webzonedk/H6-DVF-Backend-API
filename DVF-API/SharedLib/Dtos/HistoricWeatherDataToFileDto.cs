namespace DVF_API.SharedLib.Dtos
{



    public class HistoricWeatherDataToFileDto

    {
        public int id { get; set; }
        public float Latitude { get; set; }
        public float Longitude { get; set; }
        public double Time { get; set; }
        public float Temperature_2m { get; set; }
        public float Relative_Humidity_2m { get; set; }
        public float Rain { get; set; }
        public float Wind_Speed_10m { get; set; }
        public float Wind_Direction_10m { get; set; }
        public float Wind_Gusts_10m { get; set; }
        public float Global_Tilted_Irradiance_Instant { get; set; }
    }
}
