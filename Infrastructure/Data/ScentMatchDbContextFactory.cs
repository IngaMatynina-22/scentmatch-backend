using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ScentMatch.Infrastructure.Data;

public class ScentMatchDbContextFactory : IDesignTimeDbContextFactory<ScentMatchDbContext>
{
    public ScentMatchDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ScentMatchDbContext>();

        var connectionString =
            Environment.GetEnvironmentVariable("SCENTMATCH_CONNECTION")
            ?? "Server=localhost;Port=3306;Database=scentmatch;User=root;Password=root;";

        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 36)));

        return new ScentMatchDbContext(optionsBuilder.Options);
    }
}
