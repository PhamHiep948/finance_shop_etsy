using Asp.Versioning;
using Fin.Model.Dto.Finance.Requests;
using Fin.Presentation.Shared.Controllers;
using Fin.Service.Finance.UseCases.CategoryGroups.Commands;
using Fin.Service.Finance.UseCases.CategoryGroups.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Presentation.Finance.Controllers.v1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/category-groups")]
    public sealed class CategoryGroupsController : BaseApiController
    {
        [HttpGet]
        public async Task<IActionResult> GetAsync() => Ok(await Mediator.Send(new GetCategoryGroupsQuery()));

        [HttpPost]
        public async Task<IActionResult> CreateAsync(CategoryGroupRequest request) =>
            Ok(await Mediator.Send(new CreateCategoryGroupCommand { Name = request.Name, CategoryIds = request.CategoryIds }));

        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateAsync(long id, CategoryGroupRequest request) =>
            Ok(await Mediator.Send(new UpdateCategoryGroupCommand { Id = id, Name = request.Name, CategoryIds = request.CategoryIds }));

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteAsync(long id) => Ok(await Mediator.Send(new DeleteCategoryGroupCommand { Id = id }));
    }
}
