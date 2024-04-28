namespace DVF_API.SharedLib.Dtos
{
    public class MetaDataDto
    {
        public string? FetchDataTimer { get; set; }
        public string? DataLoadedMB { get; set; }
        public string? RamUsage { get; set; }
        public float CpuUsage { get; set; }
        public string? ConvertionTimer { get; set; }
        public string? ConvertionRamUsage { get; set; }
        public float ConvertionCpuUsage { get; set; }
        public List<WeatherDataDto>? WeatherData { get; set; }
    }
}
