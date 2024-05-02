namespace DVF_API.SharedLib.Dtos
{
    /// <summary>
    /// This class is used to store hourly data and is used to serialize the data from Json to models
    /// It is used in the HourlyDataDto class
    /// </summary>
    public class HourlyDataDto
    {
            public string[]? Time { get; set; }
            public float[]? Temperature { get; set; }
            public float[]? RelativeHumidity { get; set; }
            public float[]? Rain { get; set; }
            public float[]? WindSpeed { get; set; }
            public float[]? WindDirection { get; set; }
            public float[]? WindGusts { get; set; }
            public float[]? GlobalTiltedIrRadianceInstant { get; set; }
    }
}
