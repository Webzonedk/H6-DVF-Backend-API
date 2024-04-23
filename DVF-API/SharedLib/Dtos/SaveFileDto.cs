namespace DVF_API.SharedLib.Dtos
{
    public class SaveToFileDto
    {
        public HistoricWeatherDataDto HistoricWeatherData { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }
}
