using System.Runtime.InteropServices;

namespace DVF_API.SharedLib.Dtos
{
        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct WeatherStruct
        {
            [FieldOffset(0)]
            public long LocationId;
            [FieldOffset(8)]
            public unsafe fixed float WeatherData[8];
            [FieldOffset(0)]
            public fixed byte BinaryWeatherDataByteArray[40];
        }
}
