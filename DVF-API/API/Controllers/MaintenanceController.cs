using DVF_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DVF_API.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MaintenanceController : ControllerBase
    {

        #region fields
        private readonly IMaintenanceService _maintenanceService;
        #endregion

        #region Constructor
        public MaintenanceController(IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }
        #endregion


        [HttpPost("/DeleteData")]
        public IActionResult DeleteData([FromBody] DateTime deleteDataBeforeThisDate)
        {
            _maintenanceService.RemoveData(deleteDataBeforeThisDate);
            return Ok(new { message = "Data deleted" });
        }

        [HttpPost("/RestoreData")]
        public IActionResult RestoreData()
        {
            _maintenanceService.RestoreData();
            return Ok(new { message = "Data restored" });
        }

    }
}
