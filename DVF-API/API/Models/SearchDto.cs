namespace DVF_API.API.Models
{
    public class SearchDto
    {
        public DateTime fromDate { get; set; }
        public DateTime toDate { get; set; }
        public string address { get; set; }
        public bool toggleDB { get; set; }
        public bool toogleCard { get; set; }
    }
}
