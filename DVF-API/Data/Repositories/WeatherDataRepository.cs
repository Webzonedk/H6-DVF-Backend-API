namespace DVF_API.Data.Repositories
{
    public class WeatherDataRepository
    {
        private string _basePath;

        public WeatherDataRepository(string basePath)
        {
            _basePath = basePath; // Sæt stien til mappen, hvor vejrdatafilerne er placeret
        }


        /// <summary>
        /// Fetches weather data for a specific location for each day in the given date range.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <returns>Returns a list of weather data strings for each day in the given date range.</returns>
        public List<string> FetchWeatherDataForLocation(DateTime start, DateTime end, string latitude, string longitude)
        {
            List<string> weatherDataList = new List<string>();
            string basePath = $"{latitude}-{longitude}";
            for (DateTime date = start; date <= end; date = date.AddDays(1))
            {
                string path = Path.Combine(basePath, date.Year.ToString(), $"{date:yyyyMMdd}.txt");
                if (File.Exists(path))
                {
                    weatherDataList.Add(File.ReadAllText(path));
                }
            }
            return weatherDataList;
        }




        /// <summary>
        /// Fetches weather data for all locations for each day in the given date range.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns>Returns a list of weather data strings for each day in the given date range.</returns>
        public async Task<List<string>> FetchWeatherDataAsync(DateTime start, DateTime end)
        {
            List<Task<List<string>>> tasks = new List<Task<List<string>>>();

            foreach (DateTime day in EachDay(start, end))
            {
                tasks.Add(FetchDataForDayAsync(day));
            }

            var results = await Task.WhenAll(tasks);

            List<string> allResults = new List<string>();
            foreach (var dailyResults in results)
            {
                allResults.AddRange(dailyResults);
            }

            return allResults;
        }

        


        /// <summary>
        /// Fetches weather data for all locations for a given day.
        /// </summary>
        /// <param name="day"></param>
        /// <returns>Returns a list of weather data strings for the given day.</returns>
        private async Task<List<string>> FetchDataForDayAsync(DateTime day)
        {
            List<string> dailyResults = new List<string>();
            var directories = Directory.GetDirectories(_basePath);

            List<Task<string>> readTasks = new List<Task<string>>();
            foreach (var dir in directories)
            {
                string filePath = Path.Combine(dir, day.Year.ToString(), $"{day:yyyyMMdd}.txt");
                if (File.Exists(filePath))
                {
                    readTasks.Add(File.ReadAllTextAsync(filePath));
                }
            }

            var contents = await Task.WhenAll(readTasks);
            dailyResults.AddRange(contents);

            return dailyResults;
        }




        /// <summary>
        /// Finds an enumerable of each day in the given date range.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns>Returns an enumerable of each day in the given date range.</returns>
        private IEnumerable<DateTime> EachDay(DateTime start, DateTime end)
        {
            for (var day = start.Date; day <= end; day = day.AddDays(1))
            {
                yield return day;
            }
        }

    }
}
