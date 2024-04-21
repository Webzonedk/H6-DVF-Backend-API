using System.ComponentModel.DataAnnotations;

namespace DVF_API.Data.Models
{
    public class Location
    {
      
        
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string StreetName { get; set; }
        public string StreetNumber { get; set; }
        public int CityId { get; set; }
        public  City City { get; set; }

        
    }
}
