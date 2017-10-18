using System;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DarkStatsCore.Data
{
    public class DarkStatsDbContext : DbContext
    {
        public DbSet<TrafficStats> TrafficStats { get; set; }
        public DbSet<Settings> Settings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			optionsBuilder.UseSqlite("Data Source=db/darkstats.db");
            optionsBuilder.EnableSensitiveDataLogging();
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
            modelBuilder.Entity<TrafficStats>()
                        .HasKey(t => new { t.Ip, t.Day });
		}
    }

    public class TrafficStats
    {
        public string Ip { get; set; }
        public string Hostname { get; set; }
        public string Mac { get; set; }
        public long In { get; set; }
        public long Out { get; set; }
        public string LastSeen { get; set; }
        public DateTime Day { get; set; }
    }

    public class Settings
    {
        [Key]
        public string Name { get; set; }
        public string StringValue { get; set; }
        public double DoubleValue { get; set; }
        public int IntValue { get; set; }
    }
}
