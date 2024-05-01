 using DVF_API.Services.Interfaces;
using DVF_API.Services.Models;
using Microsoft.AspNetCore.Mvc;

namespace DVF_API.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherStationController : ControllerBase
    {
        private readonly IAddWeatherDataService _addWeatherDataService;

        public WeatherStationController(IAddWeatherDataService addWeatherDataService)
        {
            _addWeatherDataService = addWeatherDataService;
        }

        [HttpPost("/ApplyWeatherData")]
        public async Task<IActionResult> ApplyWeatherData([FromBody] WeatherDataFromIOT weatherDataFromIOT)
        {
            _addWeatherDataService.ApplyWeatherData(weatherDataFromIOT);
            return Ok(new { message = "Weather applied" });
        }
    }
}
