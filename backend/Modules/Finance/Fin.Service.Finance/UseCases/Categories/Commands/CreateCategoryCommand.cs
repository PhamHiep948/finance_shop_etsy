using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Categories.Commands
{
    public sealed class CreateCategoryCommand : IRequest<Response<CategoryResponse>>
    {
        public string Type { get; set; } = string.Empty;
        public string? Name { get; set; }
    }
}
