namespace DVF_API.SharedLib.Dtos
{
    /// <summary>
    /// This model is used to search for a binary file by setting the file path and the range of bytes to search in
    /// </summary>
    public class BinarySearchInFilesDto
    {
        public string? FilePath { get; set; }
        public long FromByte { get; set; }
        public long ToByte { get; set; }
    }
}
