using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MedFund.Infrastructure.Persistence;

public sealed class MedFundDbContextFactory : IDesignTimeDbContextFactory<MedFundDbContext>
{
    public MedFundDbContext CreateDbContext(string[] args)
    {
        _ = args;
        var optionsBuilder = new DbContextOptionsBuilder<MedFundDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=medfund;Username=postgres;Password=postgres");

        return new MedFundDbContext(optionsBuilder.Options);
    }
}
