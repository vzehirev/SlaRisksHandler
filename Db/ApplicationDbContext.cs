using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices.ComTypes;
using XlProcessor.Models;

namespace XlProcessor.Db
{
    class ApplicationDbContext : DbContext
    {
        private readonly string connectionString;

        public ApplicationDbContext(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public DbSet<RiskRecord> RiskRecords { get; set; }

        public DbSet<RiskStatusChange> StatusChanges { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(this.connectionString);
        }
    }
}
