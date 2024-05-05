using DVF_API.Data.Interfaces;
using DVF_API.Services.Interfaces;

namespace DVF_API.Services.ServiceImplementation
{
    /// <summary>
    /// This class is responsible for the maintenance of the data in the database and the files in the file system
    /// It is used to delete and restore data
    /// </summary>
    public class MaintenanceService : IMaintenanceService
    {

        #region Fields
        private readonly ICrudDatabaseRepository _databaseRepository;
        private readonly ICrudFileRepository _fileRepository;

        private string _baseDirectory = Environment.GetEnvironmentVariable("WEATHER_DATA_FOLDER") ?? "/Developer/DVF-WeatherFiles/weatherData/";
        private string _deletedFilesDirectory = Environment.GetEnvironmentVariable("DELETED_WEATHER_DATA_FOLDER") ?? "/Developer/DVF-WeatherFiles/deletedWeatherData/";
        #endregion




        #region Constructor
        public MaintenanceService(ICrudDatabaseRepository databaseRepository, ICrudFileRepository fileRepository)
        {
            _databaseRepository = databaseRepository;
            _fileRepository = fileRepository;
        }
        #endregion




        /// <summary>
        /// Call the delete method from the database and file repository
        /// </summary>
        /// <param name="deleteDataDto"></param>
        public void RemoveData(DateTime deleteDataDto)
        {
            _databaseRepository.DeleteOldData(deleteDataDto);
            _fileRepository.DeleteOldData(_baseDirectory, _deletedFilesDirectory, deleteDataDto);
        }




        /// <summary>
        /// call the restore method from the database and file repository
        /// </summary>
        public void RestoreData()
        {
            _databaseRepository.RestoreAllData();
            _fileRepository.RestoreAllData(_baseDirectory, _deletedFilesDirectory);
        }
    }
}
