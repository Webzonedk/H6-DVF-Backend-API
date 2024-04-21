using DVF_API.Services.Interfaces;

namespace DVF_API.Services.ServiceImplementation
{
    public class DeveloperService : IDeveloperService
    {
        private readonly IDeveloperService _developerService;

        internal DeveloperService(IDeveloperService developerService)
        {
            _developerService = developerService;
        }

        public void CreateHistoricWeatherDataAsync(bool createFiles, bool createDB)
        {
            throw new System.NotImplementedException();
        }

        public void StartSimulator()
        {
            throw new System.NotImplementedException();
        }


        public void StopSimulator()
        {
            throw new System.NotImplementedException();
        }


    }
}
