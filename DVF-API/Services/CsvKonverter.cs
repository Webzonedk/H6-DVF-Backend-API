using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace DVF_API.Services
{
    /// <summary>
    /// Provides methods to read and convert CSV data from a specified file path into a JSON format.
    /// This class focuses on extracting specific address-related fields, ensuring unique records,
    /// and saving the converted data as a JSON file.
    /// </summary>
    public class CsvConverter
    {
        /// <summary>
        /// Reads a CSV file from a predefined folder on the desktop, converts the CSV data to JSON,
        /// and saves this JSON to a file on the desktop. This method integrates functionalities of reading,
        /// parsing, and converting data to ensure that the data is handled efficiently and stored in JSON format.
        /// </summary>
        internal void ReadAndConvertCsvFile()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string csvFilePath = Path.Combine(desktopPath, "addressFilesCSV", "addresses.csv");
            string jsonFilePath = Path.Combine(desktopPath, "addresses.json");

            var addresses = ReadAndParseCSV(csvFilePath);
            string json = JsonSerializer.Serialize(addresses, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonFilePath, json);

            Console.WriteLine("Data saved as JSON. Here are the first few lines of JSON:");
            Console.WriteLine(json.Substring(0, 500));
        }




        /// <summary>
        /// Reads and parses a CSV file to extract unique address records based on specified fields.
        /// </summary>
        /// <param name="filePath">The complete file path of the CSV file to be read. This path includes the file name and its extension.</param>
        /// <returns>A list of unique 'Address' objects each representing a unique address record extracted from the CSV file. The uniqueness is determined based on the combination of street name, house number, postal code, and postal area name, along with latitude and longitude.</returns>
        internal List<Address> ReadAndParseCSV(string filePath)
        {
            var addresses = new List<Address>();
            var uniqueAddresses = new HashSet<(string, string, string, string, string, string)>();

            using (var reader = new StreamReader(filePath))
            {
                string headerLine = reader.ReadLine();
                var headers = headerLine.Split(',');

                int streetNameIndex = Array.IndexOf(headers, "vejnavn");
                int houseNumberIndex = Array.IndexOf(headers, "husnr");
                int postalCodeIndex = Array.IndexOf(headers, "postnr");
                int cityNameIndex = Array.IndexOf(headers, "postnrnavn");
                int latitudeIndex = Array.IndexOf(headers, "wgs84koordinat_bredde");
                int longitudeIndex = Array.IndexOf(headers, "wgs84koordinat_længde");

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');

                    var streetName = values[streetNameIndex];
                    var houseNumber = values[houseNumberIndex];
                    var postalCode = values[postalCodeIndex];
                    var city = values[cityNameIndex];
                    var latitude = values[latitudeIndex];
                    var longitude = values[longitudeIndex];

                    var key = (streetName, houseNumber, postalCode, city, latitude, longitude);
                    if (!uniqueAddresses.Contains(key))
                    {
                        uniqueAddresses.Add(key);
                        addresses.Add(new Address
                        {
                            StreetName = streetName,
                            HouseNumber = houseNumber,
                            PostalCode = postalCode,
                            City = city,
                            Latitude = latitude,
                            Longitude = longitude
                        });
                    }
                }
            }

            return addresses;
        }
    }




    /// <summary>
    /// Model class representing an address with street name, house number, postal code, postal area name, latitude, and longitude.
    /// </summary>
    class Address
    {
        public string StreetName { get; set; }
        public string HouseNumber { get; set; }
        public string PostalCode { get; set; }
        public string City { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
    }
}
