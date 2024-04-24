namespace DVF_API.SharedLib.Dtos
{
    public class BinaryDataFromFileDto
    {
        public string Coordinates { get; set; }
        public string YearDate { get; set; }
        public byte[] BinaryWeatherData { get; set; }
    }
}
