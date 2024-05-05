using DVF_API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DVF_API.API.Controllers
{

    /// <summary>
    /// This controller is responsible for handling the maintenance requests
    /// </summary>
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




        /// <summary>
        /// Deletes the data before the given date
        /// </summary>
        /// <param name="deleteDataBeforeThisDate"></param>
        /// <returns>A message that the data was deleted</returns>
        [HttpPost("/DeleteData")]
        public IActionResult DeleteData([FromBody][DataType(DataType.Date)] DateTime deleteDataBeforeThisDate)
        {
            _maintenanceService.RemoveData(deleteDataBeforeThisDate);
            return Ok(new { message = "Data deleted" });
        }




        /// <summary>
        /// Restores the data
        /// </summary>
        /// <returns>A message that the data was restored</returns>
        [HttpPost("/RestoreData")]
        public IActionResult RestoreData()
        {
            _maintenanceService.RestoreData();
            return Ok(new { message = "Data restored" });
        }
    }
}
