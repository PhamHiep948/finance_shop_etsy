using Fin.Infrastructure.Finance;
using Fin.Service.Finance.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Presentation.Finance.Extensions
{
    public static class FinancePresentationExtension
    {
        public static IServiceCollection AddFinanceExtension(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddFinanceInfrastructure(configuration);
            services.AddFinanceServiceLayer();
            return services;
        }
    }
}
