using DVF_API.Data.Interfaces;
using DVF_API.Services.Interfaces;

namespace DVF_API.Services.ServiceImplementation
{
    public class MaintenanceService: IMaintenanceService
    {

        private readonly IDatabaseRepository _databaseRepository;
        private readonly IFileRepository _fileRepository;
       public MaintenanceService(IDatabaseRepository databaseRepository, IFileRepository fileRepository)
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
