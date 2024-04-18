using IOT_Simulator.Models;
using Microsoft.AspNetCore.Mvc;

namespace IOT_Simulator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IOTWeatherController : ControllerBase
    {


        private readonly ILogger<IOTWeatherController> _logger;

        public IOTWeatherController(ILogger<IOTWeatherController> logger)
        {
            _logger = logger;
        }

        [HttpPost(Name = "StartDataCollection")]
        public bool StartDataCollection()
        {
            return true;
        }

        [HttpPost(Name = "StopDataCollection")]
        public bool StopDataCollection()
        {
            return true;
        }
    }
}
