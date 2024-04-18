using Microsoft.EntityFrameworkCore;

namespace DVF_API.Data.Mappers
{
    public class DvfDbContext : DbContext
    {
        public DvfDbContext(DbContextOptions<DvfDbContext> options) : base(options)
        {
        }

        public DbSet<WeatherForecast> WeatherForecasts { get; set; }


    }
}
