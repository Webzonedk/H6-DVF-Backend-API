namespace DVF_API.Domain.Interfaces
{
    /// <summary>
    /// This interface connects the SolarPositionManager with the SolarPositionService
    /// </summary>
    public interface ISolarPositionManager
    {
        (double SunAltitude, double SunAzimuth) CalculateSunAngles(DateTime dateTime, double latitude, double longitude);
    }
}
