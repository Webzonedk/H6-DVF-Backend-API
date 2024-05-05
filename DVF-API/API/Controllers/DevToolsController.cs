using DVF_API.API.Models;
using DVF_API.Services;
using DVF_API.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;

namespace DVF_API.API.Controllers
{
    /// <summary>
    /// This controller is responsible for handling the developer tools requests
    /// </summary>
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




        /// <summary>
        /// Creates the cities in the database
        /// </summary>
        /// <param name="password"></param>
        /// <returns>A message that the cities were created</returns>
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




        /// <summary>
        /// Creates the locations in the database
        /// </summary>
        /// <param name="password"></param>
        /// <returns>A message that the locations were created</returns>
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




        /// <summary>
        /// Creates the coordinates in the database
        /// </summary>
        /// <param name="password"></param>
        /// <returns>A message that the coordinates were created</returns>
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




        /// <summary>
        /// Creates the historic weather data to either the database or the files or both
        /// </summary>
        /// <param name="request"></param>
        /// <returns>A message that the historic weather data is now being processed</returns>
        [HttpPost("/CreateHistoricWeatherData")]
        public async Task<IActionResult> CreateHistoricWeatherData([FromBody] CreateHistoricWeatherDataDto request)
        {
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString()!;
            try
            {
                _= _developerService.CreateHistoricWeatherDataAsync(request.Password, clientIp, request.CreateFiles, request.CreateDB, request.StartDate, request.EndDate);
                return Ok(new { message = "Historic weather is now being processed" });
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
    }
}
