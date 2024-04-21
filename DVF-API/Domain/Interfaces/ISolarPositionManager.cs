using DVF_API.SharedLib.Dtos;

namespace DVF_API.Domain.Interfaces
{
    public interface ISolarPositionManager
    {
        WeatherDataDto CalculateSunAngles(WeatherDataDto weathterDataDto);
    }
}
