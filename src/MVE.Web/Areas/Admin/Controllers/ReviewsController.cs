using Application.Reviews.Commands;
using Application.Reviews.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReviewsController : Controller
{
    private readonly ISender _mediator;
    public ReviewsController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(bool? approved = null, int? rating = null, string? productSearch = null, int page = 1)
    {
        ViewBag.Filter = approved;
        ViewBag.Rating = rating;
        ViewBag.ProductSearch = productSearch;
        return View(await _mediator.Send(new GetAdminReviewsQuery(approved, rating, productSearch, page)));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetApproval(Guid id, bool isApproved)
    {
        var r = await _mediator.Send(new SetReviewApprovalCommand(id, isApproved));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Review status updated." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Index));
    }
}
