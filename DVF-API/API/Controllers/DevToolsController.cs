using DVF_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace DVF_API.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DevToolsController : ControllerBase
    {

        private readonly ILogger<DevToolsController> _logger;

        public DevToolsController(ILogger<DevToolsController> logger)
        {
            _logger = logger;
        }

        [HttpGet("GetWeatherDataFromAPI")]
        public void GetWeatherDataFromAPI()
        {
            CsvConverter csvConverter = new CsvConverter();
            csvConverter.ReadAndConvertCsvFile();
            csvConverter.Cleanup();
        }
    }
}
