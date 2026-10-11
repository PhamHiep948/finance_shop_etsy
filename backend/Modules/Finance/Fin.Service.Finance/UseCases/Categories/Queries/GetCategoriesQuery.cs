using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Categories.Queries
{
    public sealed class GetCategoriesQuery : IRequest<Response<List<CategoryResponse>>>
    {
        public string Type { get; set; } = string.Empty;
    }
}
