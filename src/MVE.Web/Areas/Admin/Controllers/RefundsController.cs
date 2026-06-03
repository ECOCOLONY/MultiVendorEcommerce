using Application.Refunds.Commands;
using Application.Refunds.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class RefundsController : Controller
{
    private readonly ISender _mediator;
    public RefundsController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(
        bool pendingOnly = true,
        string? search = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int page = 1)
    {
        ViewBag.PendingOnly = pendingOnly;
        ViewBag.Search = search;
        ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
        ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
        return View(await _mediator.Send(new GetRefundRequestsQuery(pendingOnly, search, dateFrom, dateTo, page)));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        var r = await _mediator.Send(new ApproveRefundCommand(id));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Refund approved and payment refunded." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string? note)
    {
        var r = await _mediator.Send(new RejectRefundCommand(id, note));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Refund declined." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Index));
    }
}
