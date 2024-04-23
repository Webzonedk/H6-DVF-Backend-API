using DVF_API.Services;
using DVF_API.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
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
        public async Task<IActionResult> CreateCities([FromHeader(Name = "X-Password")] string password)
        {
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString()!;
            try
            {
                await _developerService.CreateCities(password, clientIp);
                return Ok(new { message = "Cities created" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(429, new { message = ex.Message });
            }
        }




        [HttpPost("/CreateLocations")]
        public async Task<IActionResult> CreateLocations([FromHeader(Name = "X-Password")] string password)
        {
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString()!;
            try
            {
                await _developerService.CreateLocations(password, clientIp);
                return Ok(new { message = "Locations created" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(429, new { message = ex.Message });
            }
        }




        [HttpPost("/CreateCoordinates")]
        public async Task<IActionResult> CreateCoordinates([FromHeader(Name = "X-Password")] string password)
        {
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString()!;
            try
            {
                await _developerService.CreateCoordinates(password, clientIp);
                return Ok(new { message = "Coordinates created" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(429, new { message = ex.Message });
            }

        }




        [HttpPost("/CreateHistoricWeatherData")]
        public async Task<IActionResult> CreateHistoricWeatherData([FromQuery] bool createFiles, bool createDB, [FromHeader(Name = "X-Password")] string password)
        {
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString()!;
            try
            {
                await _developerService.CreateHistoricWeatherDataAsync(password, clientIp, createFiles, createDB);
                return Ok(new { message = "Historic weather data created" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(429, new { message = ex.Message });
            }
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
