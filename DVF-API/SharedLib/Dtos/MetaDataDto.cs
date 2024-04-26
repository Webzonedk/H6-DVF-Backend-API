namespace DVF_API.SharedLib.Dtos
{
    public class MetaDataDto
    {
        public float FetchDataTimer { get; set; }
        public string? DataLoadedMB { get; set; }
        public string? RamUsage { get; set; }
        public float CpuUsage { get; set; }
        public List<WeatherDataDto>? WeatherData { get; set; }
    }
}
