using Fin.Presentation.Finance.Extensions;
using Fin.Presentation.Shared.Extensions;
using Fin.Presentation.Shared.Options;

var builder = WebApplication.CreateBuilder(args);

// Etsy keys live in a separate, git-ignored file; editing it takes effect without restarting the API.
builder.Configuration.AddJsonFile("etsy.settings.json", optional: true, reloadOnChange: true);

var cfg = builder.Configuration;
var services = builder.Services;

// Browsers send Origin without a trailing slash, so a configured "http://host:5500/" would never match.
var origins = (cfg.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .Select(o => o.Trim().TrimEnd('/'))
    .Where(o => o.Length > 0)
    .ToArray();
services.AddCors(o => o.AddPolicy("Frontends", p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

services.Configure<AppSettingOption>(cfg.GetSection("AppSetting"));
services.AddFinanceExtension(cfg);
services.AddControllers();
services.AddApiVersioningExtension();
services.AddSwaggerExtension();

var app = builder.Build();

app.UseErrorHandlingMiddleware();
// The API also serves the plain HTML / JS front end, so the whole app runs at http://localhost:5000.
app.UseStaticFrontend(app.Environment, cfg["Frontend:Path"]);
app.UseRouting();
app.UseCors("Frontends");

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerExtension();
}

app.MapControllers();

await app.RunAsync();
