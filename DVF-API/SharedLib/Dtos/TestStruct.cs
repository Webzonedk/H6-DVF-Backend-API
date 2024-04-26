using System.Runtime.InteropServices;

namespace DVF_API.SharedLib.Dtos
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FloatAndBytes
    {
        [FieldOffset(0)]
        public float FloatValue;

        [FieldOffset(0)]
        public byte Byte1;

        [FieldOffset(1)]
        public byte Byte2;

        [FieldOffset(2)]
        public byte Byte3;

        [FieldOffset(3)]
        public byte Byte4;
    }

    class Program
    {
        static void Main()
        {
            var example = new FloatAndBytes();
            example.FloatValue = 1.0f;  // Set the float value

            // Print each byte of the float
            Console.WriteLine($"Byte1: {example.Byte1}");
            Console.WriteLine($"Byte2: {example.Byte2}");
            Console.WriteLine($"Byte3: {example.Byte3}");
            Console.WriteLine($"Byte4: {example.Byte4}");
        }
    }
}
