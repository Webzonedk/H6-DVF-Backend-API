using DVF_API.Domain.Interfaces;
using DVF_API.SharedLib.Dtos;

namespace DVF_API.Domain.BusinessLogic
{
    public class BinaryConversionManager : IBinaryConversionManager
    {


        public BinaryConversionManager()
        {
            
        }



        public WeatherDataFileDto ConvertDataFromBinary(WeatherDataFileDto weatherDataFileDto)
        {
            return new WeatherDataFileDto
            {

            };
        }
    }
}
