namespace DVF_API.SharedLib.Dtos
{
    public class BinaryDataFromFileDto
    {
        public int LocationId { get; set; }
        public string Coordinates { get; set; }
        public string Address { get; set; }
        public string YearDate { get; set; }
        public byte[] BinaryWeatherData { get; set; }
    }
}
