using DVF_API.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace DVF_API.Data.Mappers
{
    public class DvfDbContext : DbContext
    {

        public DvfDbContext(DbContextOptions<DvfDbContext> options) : base(options)
        {
        }

        public DbSet<City> Cities { get; set; }
        public DbSet<Location> Locations { get; set; }
        public DbSet<WeatherData> WeatherDatas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Konfigurationer og relationer
            modelBuilder.Entity<Location>()
                .HasOne(l => l.City)
                .WithMany(c => c.Locations)
                .HasForeignKey(l => l.CityId);

            modelBuilder.Entity<WeatherData>()
                .HasOne(w => w.Location)
                .WithMany(l => l.WeatherDatas)
                .HasForeignKey(w => w.LocationId);
        }
    }
}
