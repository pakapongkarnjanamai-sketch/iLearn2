using iLearn.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Globalization;

namespace iLearn.Infrastructure.Persistence
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(GetConnectionString());

            return new AppDbContext(
                optionsBuilder.Options,
                new DesignTimeDateTimeService(),
                new DesignTimeCurrentUserService());
        }

        private static string GetConnectionString()
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }

            return "Server=(localdb)\\mssqllocaldb;Database=iLearn2DesignTime;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        private sealed class DesignTimeDateTimeService : IDateTime
        {
            public DateTime Now => DateTime.UtcNow;
            public CultureInfo CultureInfo => CultureInfo.InvariantCulture;
            public DateTime UnixTime => DateTime.UnixEpoch;
        }

        private sealed class DesignTimeCurrentUserService : ICurrentUserService
        {
            public string UserId => "design-time";
            public string FullName => "design-time";
            public bool IsAuthenticated => false;
            public int? DivisionId => null;
            public string? DivisionName => null;
            public bool IsSuperAdmin => true;
        }
    }
}