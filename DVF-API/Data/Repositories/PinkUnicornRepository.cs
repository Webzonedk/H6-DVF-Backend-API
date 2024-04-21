using System;
using System.Data.SqlClient;
using DVF_API.Data.Models;

namespace DVF_API.Data.Repositories
{
    public class PinkUnicornRepository
    {
        private readonly IConfiguration configuration;
        private readonly string connectionString;

        public PinkUnicornRepository(IConfiguration _configuration)
        {
            configuration = _configuration;
            connectionString = configuration.GetConnectionString("WeatherDataDb");
        }

        public void InsertLocation(Location location)
        {
            string query = @"INSERT INTO Locations (StreetName, HouseNumber, PostalCode, City, Latitude, Longitude)
                             VALUES (@StreetName, @HouseNumber, @PostalCode, @City, @Latitude, @Longitude)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@StreetName", location.StreetName);
                    command.Parameters.AddWithValue("@HouseNumber", location.StreetNumber);
                    command.Parameters.AddWithValue("@PostalCode", location.City.PostalCode);
                    command.Parameters.AddWithValue("@City", location.City);
                    command.Parameters.AddWithValue("@Latitude", location.Latitude);
                    command.Parameters.AddWithValue("@Longitude", location.Longitude);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                        Console.WriteLine("Location inserted successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error inserting location: {ex.Message}");
                    }
                }
            }
        }

        public void InsertCity(City city)
        {
            string query = @"INSERT INTO Cities (PostalCode, City)
                             VALUES (@PostalCode, @City)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PostalCode", city.PostalCode);
                    command.Parameters.AddWithValue("@City", city.Name);

                    try
                    {
                        connection.Open();
                        command.ExecuteNonQuery();
                        Console.WriteLine("City inserted successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error inserting city: {ex.Message}");
                    }
                }
            }
        }
    }
}

