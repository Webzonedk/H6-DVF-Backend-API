namespace DVF_API.SharedLib.Dtos
{
    /// <summary>
    /// This model is used to search for weather data
    /// </summary>
    public class SearchDto
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public List<string> Coordinates { get; set; }
        public bool ToggleDB { get; set; }
    }
}
