using Dev_API.Models;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Dev_API.Managers
{

    /// <summary>
    /// Provides methods to read and convert CSV data from a specified file path into a JSON format.
    /// This class focuses on extracting specific address-related fields, ensuring unique records,
    /// and saving the converted data as a JSON file in a project-specific 'Data' directory.
    /// </summary>
    public class CsvConverter
    {

        /// <summary>
        /// Reads CSV data from a specified file path, parses it into Address objects ensuring they are unique, 
        /// converts these objects into a JSON formatted string, and writes this string to a new JSON file.
        /// This method is responsible for orchestrating the reading, processing, and saving of address data.
        /// </summary>
        private const int MaxRandomAddresses = 100000;

        public void ReadAndConvertCsvFile()
        {
            string dataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Sources");
            string csvFilePath = Path.Combine(dataFolderPath, "addresses.csv");
            string jsonAllFilePath = Path.Combine(dataFolderPath, "locationsAll.json");
            string jsonSelectedFilePath = Path.Combine(dataFolderPath, "locationsSelected.json");

            if (!File.Exists(csvFilePath))
            {
                Console.WriteLine("CSV file not found: " + csvFilePath);
                return;
            }

            var addresses = ReadAndParseCSV(csvFilePath).ToList();
            SaveDataAsJson(addresses, jsonAllFilePath);

            var selectedAddresses = SelectRandomAddresses(addresses);
            SaveDataAsJson(selectedAddresses, jsonSelectedFilePath);

            ExtractAndSaveUniqueCoordinates(selectedAddresses, "UniqueCoordinatesSelected.json");
            ExtractAndSaveUniqueCoordinates(addresses, "UniqueCoordinatesAll.json");
        }



        private List<Address> SelectRandomAddresses(List<Address> addresses)
        {
            var random = new Random();
            return addresses.OrderBy(x => random.Next()).Take(MaxRandomAddresses).ToList();
        }




        private void ExtractAndSaveUniqueCoordinates(IEnumerable<Address> addresses, string fileName)
        {
            string dataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Sources");
            string jsonOutputPath = Path.Combine(dataFolderPath, fileName);

            var uniqueCoordinates = addresses.Select(a => FormatCoordinate(a.Latitude) + "-" + FormatCoordinate(a.Longitude)).ToHashSet();
            SaveDataAsJson(uniqueCoordinates, jsonOutputPath);
        }





        private string FormatCoordinate(string coordinate)
        {
            int dotIndex = coordinate.IndexOf('.');
            if (dotIndex == -1)
            {
                // Hvis der ikke er noget punktum, antages det at være et helt tal, og derfor tilføjes .00000000
                return coordinate.PadLeft(3, '0') + ".00000000";
            }

            string beforeDot = coordinate.Substring(0, dotIndex);
            string afterDot = coordinate.Substring(dotIndex + 1);

            if (beforeDot.Length < 2)
            {
                beforeDot = beforeDot.PadLeft(2, '0'); // Tilføj '0' foran hvis nødvendigt
            }

            if (afterDot.Length < 8)
            {
                afterDot = afterDot.PadRight(8, '0'); // Tilføj '0' bagved indtil der er 8 cifre
            }
            else if (afterDot.Length > 8)
            {
                afterDot = afterDot.Substring(0, 8); // Klip til 8 cifre hvis der er flere
            }

            return beforeDot + '.' + afterDot;
        }




        /// <summary>
        /// Parses a CSV file into an enumerable collection of Address objects based on specific fields.
        /// Ensures that each address is unique by comparing their geographic coordinates.
        /// <param name="filePath">The path to the CSV file to be parsed.</param>
        /// <returns>An IEnumerable of Address objects with unique geographic coordinates.</returns>
        /// </summary>
        private IEnumerable<Address> ReadAndParseCSV(string filePath)
        {
            var uniqueLocations = new HashSet<string>();

            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            {
                string headerLine = reader.ReadLine();

                var headers = SplitCsvLine(headerLine);

                int streetNameIndex = headers.IndexOf("vejnavn");
                int houseNumberIndex = headers.IndexOf("husnr");
                int postalCodeIndex = headers.IndexOf("postnr");
                int cityNameIndex = headers.IndexOf("postnrnavn");
                int latitudeIndex = headers.IndexOf("wgs84koordinat_bredde");
                int longitudeIndex = headers.IndexOf("wgs84koordinat_længde");

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    var values = SplitCsvLine(line);

                    if (values.Count >= 6)
                    {
                        var location = $"{values[latitudeIndex]}-{values[longitudeIndex]}";
                        if (uniqueLocations.Add(location))
                        {
                            yield return new Address
                            {
                                StreetName = values[streetNameIndex],
                                HouseNumber = values[houseNumberIndex],
                                PostalCode = values[postalCodeIndex],
                                City = values[cityNameIndex],
                                Latitude = values[latitudeIndex],
                                Longitude = values[longitudeIndex]
                            };
                        }
                    }
                }
            }
        }




        /// <summary>
        /// Splits a single CSV line into its constituent parts while considering encapsulated commas within quotes.
        /// Handles CSV formatting issues such as quoted strings that may contain commas.
        /// <param name="line">The CSV line to split.</param>
        /// <returns>A list of strings, each representing a field in the CSV line.</returns>
        /// </summary>
        private List<string> SplitCsvLine(string line)
        {
            var result = new List<string>();
            var currentString = new StringBuilder();
            bool insideQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    insideQuotes = !insideQuotes;
                }
                else if (c == ',' && !insideQuotes)
                {
                    result.Add(currentString.ToString().Trim());
                    currentString.Clear();
                }
                else
                {
                    currentString.Append(c);
                }
            }

            result.Add(currentString.ToString().Trim());
            return result;
        }






        /// <summary>
        /// Generates a JSON file listing unique cities and postal codes extracted from 'locations.json', sorted by postal code.
        /// This method reads the JSON file, extracts city and postal code information, removes duplicates,
        /// sorts them, and then saves the result back to a new JSON file.
        /// </summary>
        public void GenerateCityListFile()
        {
            string dataFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "Data");
            string jsonFilePath = Path.Combine(dataFolderPath, "locationsAll.json");
            string cityFilePath = Path.Combine(dataFolderPath, "Cities.json");

            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine("JSON file not found: " + jsonFilePath);
                return;
            }

            string jsonData = File.ReadAllText(jsonFilePath);
            var addresses = JsonSerializer.Deserialize<List<Address>>(jsonData);

            var cityList = addresses
                .Select(a => new { a.PostalCode, a.City })
                .Distinct()
                .OrderBy(c => c.PostalCode)
                .ToList();

            SaveDataAsJson(cityList, cityFilePath);
        }



        /// <summary>
        /// Serializes any given data into JSON format and writes it to a specified file path.
        /// This generic method can handle any type of data, making it versatile for various serialization needs.
        /// <typeparam name="T">The type of data to serialize.</typeparam>
        /// <param name="data">The data to serialize.</param>
        /// <param name="filePath">The path where the JSON file should be saved.</param>
        /// </summary>
        private void SaveDataAsJson<T>(T data, string filePath)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }





        /// <summary>
        /// Performs application cleanup tasks including garbage collection to free memory, 
        /// clearing the console window, and killing all process instances except the current one.
        /// This method is designed to be called to ensure resources are cleanly managed upon application termination.
        /// </summary>
        public void Cleanup()
        {
            // Perform garbage collection to release memory
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Clear the console window
            //Console.Clear();

            // Close all existing process windows to free up resources
            Process currentProcess = Process.GetCurrentProcess();
            foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
            {
                if (process.Id != currentProcess.Id)
                {
                    process.Kill();
                }
            }
        }
    }
}
