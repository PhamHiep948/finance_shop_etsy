using Fin.Presentation.Shared.Middlewares;
using Microsoft.AspNetCore.Builder;

namespace Fin.Presentation.Shared.Extensions
{
    public static class AppExtension
    {
        public static IApplicationBuilder UseErrorHandlingMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ErrorHandlerMiddleware>();
        }

        public static IApplicationBuilder UseSwaggerExtension(this IApplicationBuilder app)
        {
            app.UseSwagger();
            return app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Finance Shop Etsy API v1"));
        }
    }
}
