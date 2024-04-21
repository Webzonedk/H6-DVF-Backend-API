namespace DVF_API.SharedLib.Dtos
{
    public class LocationDto
    {
        public double latitude { get; set; }
        public double longitude { get; set; }
        public string StreetName { get; set; }
        public string StreetNumber { get; set; }
        public int PostalCode { get; set; }
        public string City { get; set; }

    }
}
