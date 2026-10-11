using Asp.Versioning;
using Fin.Model.Dto.Finance.Requests;
using Fin.Presentation.Shared.Controllers;
using Fin.Service.Finance.UseCases.Categories.Commands;
using Fin.Service.Finance.UseCases.Categories.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Presentation.Finance.Controllers.v1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/categories")]
    public sealed class CategoriesController : BaseApiController
    {
        [HttpGet("{type}")]
        public async Task<IActionResult> GetAsync(string type) =>
            Ok(await Mediator.Send(new GetCategoriesQuery { Type = type }));

        [HttpPost("{type}")]
        public async Task<IActionResult> CreateAsync(string type, CategoryRequest request) =>
            Ok(await Mediator.Send(new CreateCategoryCommand { Type = type, Name = request.Name }));

        [HttpPut("{type}/{id:long}")]
        public async Task<IActionResult> RenameAsync(string type, long id, CategoryRequest request) =>
            Ok(await Mediator.Send(new RenameCategoryCommand { Type = type, Id = id, Name = request.Name }));

        [HttpDelete("{type}/{id:long}")]
        public async Task<IActionResult> DeleteAsync(string type, long id) =>
            Ok(await Mediator.Send(new DeleteCategoryCommand { Type = type, Id = id }));
    }
}
