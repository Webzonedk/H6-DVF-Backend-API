namespace Dev_API.Models
{

    /// <summary>
    /// Model class representing an address with street name, house number, postal code, postal area name, latitude, and longitude.
    /// </summary>
    class Address
    {
        public string StreetName { get; set; }
        public string HouseNumber { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }
}
