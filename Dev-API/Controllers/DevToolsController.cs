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

        [HttpPost("/CreateJsonWithAllCities")]
        public void CreateJsonWithAllCities()
        {
            CsvConverter csvConverter = new CsvConverter();
            csvConverter.GenerateCityListFile();
            csvConverter.Cleanup();
        }

        [HttpPost("/CreateAllFilesAtOnce")]
        public void CreateAllFilesAtOnce()
        {
            CsvConverter csvConverter = new CsvConverter();
            csvConverter.ReadAndConvertCsvFile();
            csvConverter.GenerateCityListFile();
            csvConverter.Cleanup();
        }


        [HttpPost("/CreateHistoricWeatherData")]
        public void CreateHistoricalWeatherDataAsync()
        {

            CsvConverter csvConverter = new CsvConverter();
            HistoricWeatherDataManager historicWeatherDataManager = new HistoricWeatherDataManager();
            historicWeatherDataManager.CreateHistoricalWeatherData();

            //csvConverter.Cleanup();
        }
    }
}

