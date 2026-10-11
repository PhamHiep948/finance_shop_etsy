using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Fin.Presentation.Shared.Extensions
{
    public static class FrontendExtension
    {
        public static IApplicationBuilder UseStaticFrontend(this IApplicationBuilder app, IWebHostEnvironment environment, string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return app;
            }

            var folder = Path.GetFullPath(Path.Combine(environment.ContentRootPath, relativePath));
            if (!Directory.Exists(folder))
            {
                return app;
            }

            var files = new PhysicalFileProvider(folder);
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
            return app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = files,
                // The browser always revalidates (a 304 when unchanged), so an edited JS / CSS file shows up on a plain reload.
                OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache",
            });
        }
    }
}
