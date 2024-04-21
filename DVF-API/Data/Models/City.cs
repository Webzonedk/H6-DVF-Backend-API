using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DVF_API.Data.Models
{
    public class City
    {
        public string PostalCode { get; set; }
        public string Name { get; set; }

    }
}
