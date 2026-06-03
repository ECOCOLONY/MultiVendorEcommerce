using Application.Vendors.Commands.SetVendorApproval;
using Application.Vendors.Commands.UpdateVendorCommission;
using Application.Vendors.Queries.GetVendorById;
using Application.Vendors.Queries.GetVendorsList;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class VendorsController : Controller
{
    private readonly ISender _mediator;
    public VendorsController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(bool? approved = null, string? search = null, int page = 1)
    {
        ViewBag.Filter = approved;
        ViewBag.Search = search;
        var list = await _mediator.Send(new GetVendorsListQuery(approved, search, page));
        return View(list);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var detail = await _mediator.Send(new GetVendorByIdQuery(id));
        return View(detail);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        var r = await _mediator.Send(new SetVendorApprovalCommand(id, true));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Vendor approved." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id)
    {
        var r = await _mediator.Send(new SetVendorApprovalCommand(id, false));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Vendor approval revoked." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCommission(Guid id, decimal commissionPercent)
    {
        var r = await _mediator.Send(new UpdateVendorCommissionCommand(id, commissionPercent));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Commission updated." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Details), new { id });
    }
}
