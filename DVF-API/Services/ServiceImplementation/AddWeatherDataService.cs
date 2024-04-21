using DVF_API.Services.Interfaces;
using DVF_API.Services.Models;

namespace DVF_API.Services.ServiceImplementation
{
    public class AddWeatherDataService : IAddWeatherDataService
    {
        #region fields
        private readonly IAddWeatherDataService _addWeatherDataService;
        #endregion

        internal AddWeatherDataService(IAddWeatherDataService addWeatherDataService)
        {
            _addWeatherDataService = addWeatherDataService;
        }


        public void ApplyWeatherData(WeatherDataFromIOT weatherDataFromIOT)
        {
            // Implementér den faktiske logik her
            throw new System.NotImplementedException();
        }


    }
}
