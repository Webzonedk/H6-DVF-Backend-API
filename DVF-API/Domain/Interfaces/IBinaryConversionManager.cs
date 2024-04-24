using DVF_API.SharedLib.Dtos;

namespace DVF_API.Domain.Interfaces
{
    public interface IBinaryConversionManager
    {
        WeatherDataFileDto ConvertBinaryDataToWeatherDataFileDto(byte[] rawData);
    }
}
