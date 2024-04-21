namespace DVF_API.SharedLib.Dtos
{
    public class SearchDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<string> Coordinates { get; set; }
        public bool ToggleDB { get; set; }
    }
}
