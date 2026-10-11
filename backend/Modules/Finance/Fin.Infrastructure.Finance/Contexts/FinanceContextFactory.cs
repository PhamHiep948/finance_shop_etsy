using Fin.Infrastructure.Finance.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fin.Infrastructure.Finance.Contexts
{
    public sealed class FinanceContextFactory : IDesignTimeDbContextFactory<FinanceContext>
    {
        public FinanceContext CreateDbContext(string[] args)
        {
            // Design time only (dotnet ef migrations add): the server version is fixed so no live database is needed.
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Finance")
                ?? "Server=127.0.0.1;Port=3307;Database=finance;User ID=root;Password=";
            var options = new DbContextOptionsBuilder<FinanceContext>()
                .UseMySql(connectionString, ServerVersion.Parse(FinanceDatabaseOptions.DefaultServerVersion))
                .Options;
            return new FinanceContext(options);
        }
    }
}
