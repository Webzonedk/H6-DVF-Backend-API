using DVF_API.Domain.Interfaces;
using DVF_API.SharedLib.Dtos;

namespace DVF_API.Domain.BusinessLogic
{
    /// <summary>
    /// This class is responsible for converting of binary data.
    /// </summary>
    public class BinaryConversionManager : IBinaryConversionManager
    {


        public BinaryConversionManager()
        {

        }




        /// <summary>
        /// Converts a byte array of binary data to a WeatherDataFileDto object.
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns>Returns a WeatherDataFileDto object.</returns>
        public WeatherDataFileDto ConvertBinaryDataToWeatherDataFileDto(byte[] rawData)
        {
            using (MemoryStream ms = new MemoryStream(rawData))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                int numEntries = rawData.Length / 32; // Each entry has 8 float values, each float being 4 bytes.
                WeatherDataFileDto data = new WeatherDataFileDto
                {
                    Time = new float[numEntries],
                    Temperature_2m = new float[numEntries],
                    Relative_Humidity_2m = new float[numEntries],
                    Rain = new float[numEntries],
                    Wind_Speed_10m = new float[numEntries],
                    Wind_Direction_10m = new float[numEntries],
                    Wind_Gusts_10m = new float[numEntries],
                    Global_Tilted_Irradiance_Instant = new float[numEntries]
                };

                for (int i = 0; i < numEntries; i++)
                {
                    data.Time[i] = reader.ReadSingle();
                    data.Temperature_2m[i] = reader.ReadSingle();
                    data.Relative_Humidity_2m[i] = reader.ReadSingle();
                    data.Rain[i] = reader.ReadSingle();
                    data.Wind_Speed_10m[i] = reader.ReadSingle();
                    data.Wind_Direction_10m[i] = reader.ReadSingle();
                    data.Wind_Gusts_10m[i] = reader.ReadSingle();
                    data.Global_Tilted_Irradiance_Instant[i] = reader.ReadSingle();
                }
                return data;
            }
        }
    }
}
