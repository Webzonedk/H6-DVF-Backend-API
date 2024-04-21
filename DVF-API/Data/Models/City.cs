using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DVF_API.Data.Models
{
    public class City
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CityId { get; set; }
        public string PostalCode { get; set; }
        public string Name { get; set; }

        // Navigation properties
        public virtual ICollection<Location> Locations { get; set; } = new List<Location>();
    }
}
