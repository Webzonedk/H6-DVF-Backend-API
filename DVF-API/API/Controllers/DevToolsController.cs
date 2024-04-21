using DVF_API.Services;
using DVF_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DVF_API.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DevToolsController : ControllerBase
    {

        #region fields
        private readonly IDeveloperService _developerService;
        #endregion

        #region Constructor
        internal DevToolsController(IDeveloperService developerService)
        {
            _developerService = developerService;
        }
        #endregion

        [HttpPost("CreateHistoricWeatherData")]
        public async Task<IActionResult> CreateHistoricWeatherData([FromBody] bool createFiles, bool createDB)
        {
          _developerService.CreateHistoricWeatherDataAsync(createFiles, createDB);
            return Ok(new { message = "Historic weather data created" });
        }

        [HttpPost("StartSimulator")]
        public async Task<IActionResult> StartSimulator()
        {
          _developerService.StartSimulator();
            return Ok(new { message = "Simulator started" });
        }

        [HttpPost("StopSimulator")]
        public async Task<IActionResult> StopSimulator()
        {
          _developerService.StopSimulator();
            return Ok(new { message = "Simulator stopped" });
        }

        [HttpPost("CreateCities")]
        public async Task<IActionResult> CreateCities()
        {
            _developerService.CreateCities();
            return Ok(new { message = "Cities created" });
        }

        [HttpPost("CreateLocations")]
        public async Task<IActionResult> CreateLocations()
        {
            _developerService.CreateLocations();
            return Ok(new { message = "Locations created" });
        }

    }
}
