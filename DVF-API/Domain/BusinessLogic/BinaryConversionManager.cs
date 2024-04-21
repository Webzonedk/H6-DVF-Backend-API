using DVF_API.Domain.Interfaces;
using DVF_API.SharedLib.Dtos;

namespace DVF_API.Domain.BusinessLogic
{
    public class BinaryConversionManager : IBinaryConversionManager
    {

        private readonly IBinaryConversionManager _binaryConversionManager;

        internal BinaryConversionManager(IBinaryConversionManager binaryConversionManager)
        {
            _binaryConversionManager = binaryConversionManager;
        }



        public WeatherDataFileDto ConvertDataFromBinary(WeatherDataFileDto weatherDataFileDto)
        {
            return new WeatherDataFileDto
            {

            };
        }
    }
}
