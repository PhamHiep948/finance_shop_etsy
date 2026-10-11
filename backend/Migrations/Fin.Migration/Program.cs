using Fin.Infrastructure.Finance;
using Fin.Migration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// appsettings*.json are copied next to the binary; using that folder lets "dotnet run --project" work from anywhere.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = args, ContentRootPath = AppContext.BaseDirectory });
builder.Services.AddFinanceInfrastructure(builder.Configuration);
builder.Services.AddScoped<MigrationRunner>();

using var host = builder.Build();
using var scope = host.Services.CreateScope();
return await scope.ServiceProvider.GetRequiredService<MigrationRunner>().RunAsync(CancellationToken.None);
