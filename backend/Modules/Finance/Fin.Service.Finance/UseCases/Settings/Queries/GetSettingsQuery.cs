using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Settings.Queries
{
    public sealed class GetSettingsQuery : IRequest<Response<SettingsResponse>>
    {
    }
}
