namespace DVF_API.SharedLib.Dtos
{
    public class BinarySearchInFilesDto
    {
        public string? FilePath { get; set; }
        public long FromByte { get; set; }
        public long ToByte { get; set; }
    }
}
