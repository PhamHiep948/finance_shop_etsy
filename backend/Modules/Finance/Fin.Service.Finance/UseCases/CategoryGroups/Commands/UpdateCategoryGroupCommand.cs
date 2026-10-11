using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.CategoryGroups.Commands
{
    public sealed class UpdateCategoryGroupCommand : IRequest<Response<bool>>
    {
        public long Id { get; set; }
        public string? Name { get; set; }
        public List<long>? CategoryIds { get; set; }
    }
}
