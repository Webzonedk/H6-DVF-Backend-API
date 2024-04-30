using System.Runtime.InteropServices;

namespace DVF_API.SharedLib.Dtos
{
    /// <summary>
    /// used to carry weather data and bytes using the same memory
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct BinaryWeatherStructDto
    {
        [FieldOffset(0)]
        public long LocationId;
        [FieldOffset(8)]
        public unsafe fixed float WeatherData[8];
        [FieldOffset(0)]
        public unsafe fixed byte BinaryWeatherDataByteArray[40];
    }
}
