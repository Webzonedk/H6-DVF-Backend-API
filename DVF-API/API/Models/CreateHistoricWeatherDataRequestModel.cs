using System.ComponentModel.DataAnnotations;

namespace DVF_API.API.Models
{
    public class CreateHistoricWeatherDataRequestModel
    {
        public bool CreateFiles { get; set; }
        public bool CreateDB { get; set; }
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        public string Password { get; set; }
    }
}
