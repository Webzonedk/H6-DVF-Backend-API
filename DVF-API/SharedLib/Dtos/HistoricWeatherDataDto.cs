namespace DVF_API.SharedLib.Dtos
{
    /// <summary>
    /// This class represents the historic weather data.
    /// Contains the hourly data reference. that carries the array of hourly data.
    /// </summary>
    public class HistoricWeatherDataDto
    {
        public HourlyDataDto Hourly { get; set; }
    }
}
