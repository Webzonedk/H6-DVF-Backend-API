using DVF_API.Data.Interfaces;
using DVF_API.SharedLib.Dtos;
using System.Globalization;
using System.Data.SqlClient;


namespace DVF_API.Data.Repositories
{
    public class CrudDatabaseRepository : IDataRepository, ILocationRepository
    {
        
        private readonly IConfiguration configuration;
        private readonly string connectionString;

        public CrudDatabaseRepository(IConfiguration _configuration)
        {

        configuration = _configuration;
            connectionString = configuration.GetConnectionString("WeatherDataDb");
        }

        public MetaDataDto FetchWeatherData(SearchDto searchDto)
        {
            // Parse and filter by coordinates
            var coordinates = searchDto.Coordinates
                .Select(coord => {
                    var parts = coord.Split('-');
                    return new { Latitude = double.Parse(parts[0], CultureInfo.InvariantCulture), Longitude = double.Parse(parts[1], CultureInfo.InvariantCulture) };
                }).ToList();

            // Query the database
            var query = _dvfDbContext.WeatherDatas
                .Include(wd => wd.Location)
                    .ThenInclude(l => l.City)
                .Where(wd => coordinates.Any(c => c.Latitude == wd.Location.Latitude && c.Longitude == wd.Location.Longitude))
                .Where(wd => wd.DateAndTime >= searchDto.FromDate && wd.DateAndTime <= searchDto.ToDate);

            // Project to WeatherDataDto (Assuming you have a mapper or manually map the fields)
            var weatherDataDtos = query.Select(wd => new WeatherDataDto
            {
                Latitude = (float)wd.Location.Latitude,
                Longitude = (float)wd.Location.Longitude,
                TemperatureC = wd.TemperatureC,
                WindSpeed = wd.WindSpeed,
                WindDirection = wd.WindDirection,
                WindGust = wd.WindGust,
                RelativeHumidity = wd.RelativeHumidity,
                Rain = wd.Rain,
                GlobalTiltedIrRadiance = wd.GlobalTiltedIrRadiance,
                DateAndTime = wd.DateAndTime,
                // Additional mappings can be added here
            }).ToList();

            // Populate the MetaDataDto
            return new MetaDataDto
            {
                WeatherData = weatherDataDtos,
            };
        }

        public void DeleteOldData(DateTime deleteWeatherDataBeforeThisDate)
        {
            throw new NotImplementedException();
        }

        public List<string> FetchLoactionCoordinates(int fromIndex, int toIndex)
        {
            throw new NotImplementedException();
        }

        public int FetchLocationCount(string partialAddress)
        {
            throw new NotImplementedException();
        }

        public List<string> FetchMatchingAddresses(string partialAddress)
        {
            throw new NotImplementedException();
        }

        public void InsertData(WeatherDataFromIOTDto weatherDataFromIOT)
        {
            

           try{

           }
           catch{

           }
        }

        public void RestoreAllData()
        {
            throw new NotImplementedException();
        }

    }
}
