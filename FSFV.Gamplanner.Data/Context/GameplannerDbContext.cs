using FSFV.Gamplanner.Data.Model;
using FSFV.Gamplanner.Data.Model.Intermediary;
using FSFV.Gamplanner.Data.Util;
using Microsoft.EntityFrameworkCore;
using System;

namespace FSFV.Gamplanner.Data.Context
{
    public class GameplannerDbContext : DbContext
    {

        public DbSet<Team> Teams { get; set; }
        public DbSet<Contest> Contests { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // TODO connection string for locale db
            var server = Environment.MachineName.StartsWith("DESKTOP") ? "DESKTOP" : "LAPTOP";
            optionsBuilder.UseSqlServer(@"Server=" + server + ";Database=Dev;Trusted_Connection=True;");

            // TODO configure table columns
            // e.g. prevent VARCHAR(max) and nullable FK etc

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.RemovePluralizingTableNameConvention();

            // Team to Contest many to many relationship
            // This maybe unnecessary later on, see here:
            // https://github.com/dotnet/efcore/issues/1368
            modelBuilder.Entity<ContestTeam>()
                .HasKey(e => new { e.TeamID, e.ContestID });
            modelBuilder.Entity<ContestTeam>()
                .HasOne(e => e.Team)
                .WithMany(t => t.ContestTeams)
                .HasForeignKey(e => e.TeamID);
            modelBuilder.Entity<ContestTeam>()
                .HasOne(e => e.Contest)
                .WithMany(c => c.ContestTeams)
                .HasForeignKey(e => e.ContestID);

            }
    }
}
