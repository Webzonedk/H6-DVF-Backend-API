using IOT_Simulator.Interfaces;
using IOT_Simulator.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace IOT_Simulator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IOTWeatherController : ControllerBase
    {
        private readonly ISimulatorIOTManager _simulatorIOTManager;

        public IOTWeatherController(ISimulatorIOTManager simulatorIOTManager)
        {
            _simulatorIOTManager = simulatorIOTManager;
        }


        [HttpPost(Name = "StartDataCollection")]
        public IActionResult StartDataCollection()
        {
            _simulatorIOTManager.Start();
            return Ok();
        }

        [HttpPost(Name = "StopDataCollection")]
        public IActionResult StopDataCollection()
        {
            _simulatorIOTManager.Stop();
            return Ok();
        }

    }
}
