using IOT_Simulator.Interfaces;
using IOT_Simulator.Models;

namespace IOT_Simulator.Managers
{
    public class SimulatorIOTManager: ISimulatorIOTManager
    {
        private readonly ISimulatorIOTManager _simulatorIOTManager;

        public SimulatorIOTManager(ISimulatorIOTManager simulatorIOTManager)
        {
            _simulatorIOTManager = simulatorIOTManager;
        }

        private bool _run = false;

        public void Start()
        {
            _run = true;
            //throw new System.NotImplementedException();
        }
        public void Stop()
        {
            _run = false;
        }
       

        //internal WeatherDataAPIJson GetWeatherFromAPI()
        //{
        //    return new WeatherDataAPIJson();
        //}

        //internal List<string> GetCoordinatesFromDvfAPI()
        //{
        //    return new List<string>();
        //}

        //internal List<WeatherDataDtoIOT> GenerateDataForAllLocations(WeatherDataAPIJson weatherDataAPIJson)
        //{
        //    return  new List<WeatherDataDtoIOT>();
        //}

        //internal List<WeatherDataDtoIOT> SendDataToDvfAPI(List<WeatherDataDtoIOT> dataToSendToDvfAPI)
        //{
        //    return new List<WeatherDataDtoIOT>();
        //}
    }
}
