using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Fin.Presentation.Shared.Extensions
{
    public static class ServiceExtension
    {
        public static IServiceCollection AddApiVersioningExtension(this IServiceCollection services)
        {
            services
                .AddApiVersioning(options =>
                {
                    options.DefaultApiVersion = new ApiVersion(1, 0);
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.ReportApiVersions = true;
                })
                .AddMvc()
                .AddApiExplorer(options =>
                {
                    // "'v'VVV" yields group names like "v1" so Swagger documents line up with the URL segment.
                    options.GroupNameFormat = "'v'VVV";
                    options.SubstituteApiVersionInUrl = true;
                });

            return services;
        }

        public static IServiceCollection AddSwaggerExtension(this IServiceCollection services)
        {
            services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Finance Shop Etsy API",
                Description = "Thu chi, báo cáo, tỷ giá và đồng bộ đơn Etsy của shop.",
            }));

            return services;
        }
    }
}
