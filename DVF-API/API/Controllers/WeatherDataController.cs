using DVF_API.Data.Models;
using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DVF_API.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherDataController : ControllerBase
    {

        #region fields
        private readonly IDataService _dataService;
        #endregion

        #region Constructor
        internal WeatherDataController(IDataService dataService)
        {
            _dataService = dataService;
        }
        #endregion

        [HttpPost(Name = "GetAddress")]
        public IEnumerable<string> GetAddress()
        {
            return null;
        }

        [HttpGet(Name  = "GetLocationCount")]
        public int GetLocationCount()
        {
            return 0;
        }


        [HttpPost(Name = "GetLocations")]
        public IEnumerable<string> GetLocations(int fromIndex, int toIndex)
        {
            return null;
        }

        [HttpGet(Name = "GetWeatherData")]
        public MetaDataDto GetWeatherData(SearchDto seachDto)
        {
            return null;
        }
    }
}
