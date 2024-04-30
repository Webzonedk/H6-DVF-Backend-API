using DVF_API.SharedLib.Dtos;

namespace DVF_API.Domain.Interfaces
{
    public interface ISolarPositionManager
    {
        (double SunAltitude, double SunAzimuth) CalculateSunAngles(DateTime dateTime, double latitude, double longitude);
    }
}
