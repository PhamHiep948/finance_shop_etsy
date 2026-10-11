using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.Categories.Commands
{
    public sealed class DeleteCategoryCommand : IRequest<Response<bool>>
    {
        public string Type { get; set; } = string.Empty;
        public long Id { get; set; }
    }
}
