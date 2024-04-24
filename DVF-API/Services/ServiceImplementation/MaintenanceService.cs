using DVF_API.Data.Interfaces;
using DVF_API.Services.Interfaces;

namespace DVF_API.Services.ServiceImplementation
{
    public class MaintenanceService: IMaintenanceService
    {

        private readonly ICrudDatabaseRepository _databaseRepository;
        private readonly ICrudFileRepository _fileRepository;
       public MaintenanceService(ICrudDatabaseRepository databaseRepository, ICrudFileRepository fileRepository)
        {
            _databaseRepository = databaseRepository;
            _fileRepository = fileRepository;
        }



        public void RemoveData(DateTime deleteDataDto) 
        { 
            //Method to delete data
            _databaseRepository.DeleteOldData(deleteDataDto);
        }
        public void RestoreData()
        {
            _databaseRepository.RestoreAllData();
        }
    }
}
