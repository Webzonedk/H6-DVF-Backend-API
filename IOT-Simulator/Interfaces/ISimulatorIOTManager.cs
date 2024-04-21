namespace IOT_Simulator.Interfaces
{
    public interface ISimulatorIOTManager
    {
        void Start();
        void Stop();
        //internal WeatherDataAPIJson GetWeatherFromAPI();
        //internal List<string> GetCoordinatesFromDvfAPI();
        //internal List<WeatherDataDtoIOT> GenerateDataForAllLocations(WeatherDataAPIJson weatherDataAPIJson);
        //internal List<WeatherDataDtoIOT> SendDataToDvfAPI(List<WeatherDataDtoIOT> dataToSendToDvfAPI);

    }
}
