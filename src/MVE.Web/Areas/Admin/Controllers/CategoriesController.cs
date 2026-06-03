using Application.Categories.Commands.CreateCategory;
using Application.Categories.Commands.DeleteCategory;
using Application.Categories.Commands.UpdateCategory;
using Application.Categories.Queries.GetCategoriesList;
using Application.Categories.Queries.GetCategoryById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web.Models.ViewModels.Admin.Categories;

namespace Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoriesController : Controller
{
    private readonly ISender _mediator;
    public CategoriesController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index()
    {
        var list = await _mediator.Send(new GetCategoriesListQuery());
        return View(list);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var detail = await _mediator.Send(new GetCategoryByIdQuery(id));
        return View(detail);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new CategoryFormViewModel { IsActive = true };
        await PopulateParents(vm, null);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel vm)
    {
        if (!ModelState.IsValid) { await PopulateParents(vm, null); return View(vm); }

        var result = await _mediator.Send(new CreateCategoryCommand(
            vm.Name, vm.Description, vm.IconUrl, vm.ParentCategoryId, vm.DisplayOrder, vm.IsActive));

        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);
            await PopulateParents(vm, null);
            return View(vm);
        }

        TempData["Success"] = $"Category '{vm.Name}' created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var d = await _mediator.Send(new GetCategoryByIdQuery(id));
        var vm = new CategoryFormViewModel
        {
            Id = d.Id, Name = d.Name, Description = d.Description, IconUrl = d.IconUrl,
            ParentCategoryId = d.ParentCategoryId, DisplayOrder = d.DisplayOrder, IsActive = d.IsActive
        };
        await PopulateParents(vm, d.Id);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryFormViewModel vm)
    {
        if (vm.Id is null) return BadRequest();
        if (!ModelState.IsValid) { await PopulateParents(vm, vm.Id); return View(vm); }

        var result = await _mediator.Send(new UpdateCategoryCommand(
            vm.Id.Value, vm.Name, vm.Description, vm.IconUrl, vm.ParentCategoryId, vm.DisplayOrder, vm.IsActive));

        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);
            await PopulateParents(vm, vm.Id);
            return View(vm);
        }

        TempData["Success"] = $"Category '{vm.Name}' updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var d = await _mediator.Send(new GetCategoryByIdQuery(id));
        return View(d);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var result = await _mediator.Send(new DeleteCategoryCommand(id));
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join(" ", result.Errors);
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = "Category deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateParents(CategoryFormViewModel vm, Guid? excludeId)
    {
        var all = await _mediator.Send(new GetCategoriesListQuery());
        vm.ParentCategories = all
            .Where(c => excludeId is null || c.Id != excludeId)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name });
    }
}
