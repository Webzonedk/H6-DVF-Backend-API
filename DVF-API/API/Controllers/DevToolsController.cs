using DVF_API.Services;
using DVF_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;

namespace DVF_API.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DevToolsController : ControllerBase
    {



        #region fields
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly IDeveloperService _developerService;
        private const string VerySecretPassword = "2^aQeqnZoTH%PDgiFpRDa!!kL#kPLYWL3*D9g65fxQt@HYKpfAaWDkjS8sGxaCUEUVLrgR@wdoF";
        private static Dictionary<string, (DateTime lastAttempt, int attemptCount)> _loginAttempts = new();

        #endregion

        #region Constructor
        public DevToolsController(IDeveloperService developerService)
        {
            _developerService = developerService;
        }
        #endregion




        [HttpPost("/CreateCities")]
        public async Task<IActionResult> CreateCities()
        {
            //await _developerService.CreateCities();
            return Ok(new { message = "Cities created" });
        }




        [HttpPost("/CreateLocations")]
        public async Task<IActionResult> CreateLocations()
        {
            await _developerService.CreateLocations();
            return Ok(new { message = "Locations created" });
        }




        [HttpPost("/CreateCoordinates")]
        public async Task<IActionResult> CreateCoordinates()
        {
            await _developerService.CreateCoordinates();
            return Ok(new { message = "Coordinates created" });
        }




        [HttpPost("/CreateHistoricWeatherData")]
        public async Task<IActionResult> CreateHistoricWeatherData([FromQuery] bool createFiles, bool createDB, [FromHeader(Name = "X-Password")] string password)
        {
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString()!;

            if (_loginAttempts.ContainsKey(clientIp))
            {
                var (lastAttempt, attemptCount) = _loginAttempts[clientIp];
                if (DateTime.UtcNow - lastAttempt < TimeSpan.FromMinutes(5) && attemptCount >= 5)
                {
                    return StatusCode(429, new { message = "Too many failed attempts. Please try again later." });
                }
            }

            if (password != VerySecretPassword)
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
            await _developerService.CreateHistoricWeatherDataAsync(createFiles, createDB);
            return Ok(new { message = "Historic weather data created" });
        }




        [HttpPost("/StartSimulator")]
        public async Task<IActionResult> StartSimulator()
        {
            var response = await _httpClient.GetAsync("https://iot-api.weblion.dk/StartSimulator");
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new { message = $"Failed to start simulator: {response.StatusCode}" });
            }
            return Ok(new { message = "Simulator started" });
        }




        [HttpPost("/StopSimulator")]
        public async Task<IActionResult> StopSimulator()
        {
            var response = await _httpClient.GetAsync("https://iot-api.weblion.dk/StopSimulator");
            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new { message = $"Failed to stop simulator: {response.StatusCode}" });
            }
            return Ok(new { message = "Simulator stopped" });
        }
    }
}
