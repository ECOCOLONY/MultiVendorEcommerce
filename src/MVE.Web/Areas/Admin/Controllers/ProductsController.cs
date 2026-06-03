using Application.Categories.Queries.GetCategoryLookup;
using Application.Products.Commands.SetProductApproval;
using Application.Products.Queries.GetProductForAdmin;
using Application.Products.Queries.GetProductsForAdmin;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ProductsController : Controller
{
    private readonly ISender _mediator;
    public ProductsController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(
        ProductApprovalStatus? status = null,
        string? search = null,
        Guid? categoryId = null,
        string? vendorSearch = null,
        int page = 1)
    {
        ViewBag.Filter = status;
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.VendorSearch = vendorSearch;
        ViewBag.Categories = await _mediator.Send(new GetCategoryLookupQuery());
        var list = await _mediator.Send(new GetProductsForAdminQuery(status, search, categoryId, vendorSearch, page));
        return View(list);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var p = await _mediator.Send(new GetProductForAdminQuery(id));
        return View(p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        var r = await _mediator.Send(new SetProductApprovalCommand(id, ProductApprovalStatus.Approved));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Product approved and published." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id)
    {
        var r = await _mediator.Send(new SetProductApprovalCommand(id, ProductApprovalStatus.Rejected));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Product rejected." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Details), new { id });
    }
}
