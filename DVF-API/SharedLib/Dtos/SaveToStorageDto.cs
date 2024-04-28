namespace DVF_API.SharedLib.Dtos
{
    public class SaveToStorageDto
    {
        public long LocationId { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public HistoricWeatherDataDto HistoricWeatherData { get; set; }
    }
}
