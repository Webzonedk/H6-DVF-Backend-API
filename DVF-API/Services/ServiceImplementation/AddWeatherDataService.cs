using DVF_API.Data.Interfaces;
using DVF_API.Services.Interfaces;
using DVF_API.Services.Models;

namespace DVF_API.Services.ServiceImplementation
{
    public class AddWeatherDataService : IAddWeatherDataService
    {
        #region fields
        private readonly ICrudDatabaseRepository _databaseRepository;
        private readonly ICrudFileRepository _fileRepository;
        #endregion

       public AddWeatherDataService(ICrudDatabaseRepository databaseRepository, ICrudFileRepository fileRepository)
        {
            _databaseRepository = databaseRepository;
            _fileRepository = fileRepository;
        }


        public void ApplyWeatherData(WeatherDataFromIOT weatherDataFromIOT)
        {
            // Implementér den faktiske logik her
            throw new System.NotImplementedException();
        }


    }
}
