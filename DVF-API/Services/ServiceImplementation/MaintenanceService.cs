using DVF_API.Services.Interfaces;

namespace DVF_API.Services.ServiceImplementation
{
    public class MaintenanceService: IMaintenanceService
    {
        private readonly IMaintenanceService _maintenanceService;

        internal MaintenanceService(IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }



        public void RemoveData(DateTime deleteDataDto) 
        { 
            //Method to delete data
        }
        public void RestoreData()
        {
            _maintenanceService.RestoreData();
        }
    }
}
