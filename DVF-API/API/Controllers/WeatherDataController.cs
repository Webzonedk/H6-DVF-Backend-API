using DVF_API.Services.Interfaces;
using DVF_API.SharedLib.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DVF_API.API.Controllers
{
    /// <summary>
    /// This controller is responsible for handling the weather data requests
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class WeatherDataController : ControllerBase
    {

        #region fields
        private readonly IDataService _dataService;
        #endregion




        #region Constructor
        public WeatherDataController(IDataService dataService)
        {
            _dataService = dataService;
        }
        #endregion




        /// <summary>
        /// Gets the addresses that matches the input
        /// </summary>
        /// <param name="addressInput"></param>
        /// <returns>A list of addresses that match the input</returns>
        [HttpPost("/GetAddress")]
        public async Task < IEnumerable<string>> GetAddress(string addressInput)
        {
            return await _dataService.GetAddressesFromDBMatchingInputs(addressInput);
        }




        /// <summary>
        /// Gets the count of the locations in the database
        /// </summary>
        /// <returns>An integer with the count of the locations</returns>
        [HttpGet("/GetLocationCount")]
        public Task< int> GetLocationCount()
        {
          return _dataService.CountLocations();
        }




        /// <summary>
        /// Gets the locations from the database
        /// </summary>
        /// <param name="fromIndex"></param>
        /// <param name="toIndex"></param>
        /// <returns>A dictionary with the location id and the location name</returns>
        [HttpPost("/GetLocations")]
        public async Task<Dictionary<long, string>> GetLocations(int fromIndex, int toIndex)
        {
            return await _dataService.GetLocationCoordinates(fromIndex, toIndex);
        }




        /// <summary>
        /// Gets the weather data from the database based on the searchDto
        /// </summary>
        /// <param name="searchDto"></param>
        /// <returns>A MetaDataDto object with the weather data</returns>
        [HttpPost("/GetWeatherData")]
        public async Task< MetaDataDto> GetWeatherData(SearchDto searchDto)
        {
           return await _dataService.GetWeatherDataService(searchDto);
        }
    }
}
