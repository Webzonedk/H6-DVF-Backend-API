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
        public DevToolsController(IDeveloperService developerService)
        {
            _developerService = developerService;
        }
        #endregion

        private static Dictionary<string, (DateTime lastAttempt, int attemptCount)> _loginAttempts = new();

        [HttpPost("/CreateHistoricWeatherData")]
        public async Task<IActionResult> CreateHistoricWeatherData([FromQuery] bool createFiles, bool createDB, [FromHeader(Name = "X-Password")] string password)
        {
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            const string hardcodedPassword = "2^aQeqnZoTH%PDgiFpRDa!!kL#kPLYWL3*D9g65fxQt@HYKpfAaWDkjS8sGxaCUEUVLrgR@wdoF";

            if (_loginAttempts.ContainsKey(clientIp))
            {
                var (lastAttempt, attemptCount) = _loginAttempts[clientIp];
                if (DateTime.UtcNow - lastAttempt < TimeSpan.FromMinutes(5) && attemptCount >= 5)
                {
                    return StatusCode(429, new { message = "Too many failed attempts. Please try again later." });
                }
            }

            if (password != hardcodedPassword)
            {
                if (_loginAttempts.ContainsKey(clientIp))
                {
                    _loginAttempts[clientIp] = (DateTime.UtcNow, _loginAttempts[clientIp].attemptCount + 1);
                }
                else
                {
                    _loginAttempts[clientIp] = (DateTime.UtcNow, 1);
                }
                return Unauthorized(new { message = "Unauthorized - Incorrect password" });
            }

            _loginAttempts.Remove(clientIp);
            _developerService.CreateHistoricWeatherDataAsync(createFiles, createDB);
            return Ok(new { message = "Historic weather data created" });
        }



        [HttpPost("/StartSimulator")]
        public async Task<IActionResult> StartSimulator()
        {
          _developerService.StartSimulator();
            return Ok(new { message = "Simulator started" });
        }

        [HttpPost("/StopSimulator")]
        public async Task<IActionResult> StopSimulator()
        {
          _developerService.StopSimulator();
            return Ok(new { message = "Simulator stopped" });
        }

        [HttpPost("/CreateCities")]
        public async Task<IActionResult> CreateCities()
        {
            //_developerService.CreateCities();
            return Ok(new { message = "Cities created" });
        }

        [HttpPost("/CreateLocations")]
        public async Task<IActionResult> CreateLocations()
        {
            //_developerService.CreateLocations();
            return Ok(new { message = "Locations created" });
        }

    }
}
