using Dev_API.Managers;
using Microsoft.AspNetCore.Mvc;

namespace Dev_API.Controllers
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

        [HttpPost("/CreateLocationsFromCsv")]
        public void CreateLocationsFromCsv()
        {
            CsvConverter csvConverter = new CsvConverter();
            csvConverter.ReadAndConvertCsvFile();
            csvConverter.Cleanup();
        }

        [HttpPost("/CreateJsonWithUniqueCoordinates")]
        public void CreateJsonWithUniqueCoordinates()
        {
            CsvConverter csvConverter = new CsvConverter();
            csvConverter.ExtractAndSaveUniqueCoordinates();
            csvConverter.Cleanup();
        }

        [HttpPost("/CreateJsonWithAllCities")]
        public void CreateJsonWithAllCities()
        {
            CsvConverter csvConverter = new CsvConverter();
            csvConverter.GenerateCityListFile();
            csvConverter.Cleanup();
        }

        [HttpPost("/CreateAlleFilesAtOnce")]
        public void CreateAlleFilesAtOnce()
        {
            CsvConverter csvConverter = new CsvConverter();
            csvConverter.ReadAndConvertCsvFile();
            csvConverter.ExtractAndSaveUniqueCoordinates();
            csvConverter.GenerateCityListFile();
            csvConverter.Cleanup();
        }
    }
}
