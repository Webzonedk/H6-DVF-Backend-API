using System.Runtime.InteropServices;

namespace DVF_API.SharedLib.Dtos
{
    public class BinaryWeatherStructDto
    {
        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct WeatherStruct
        {

            [FieldOffset(0)]
            public long LocationId;
            [FieldOffset(8)]
            public unsafe fixed float WeatherData[8];
            [FieldOffset(0)]
            public fixed byte BinaryWeatherDataByteArray[40];




        }
    }
}
