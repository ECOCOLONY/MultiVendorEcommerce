using Application.Reviews.Commands.CreateReview;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Authorize]
public class ReviewsController : Controller
{
    private readonly ISender _mediator;
    public ReviewsController(ISender mediator) => _mediator = mediator;

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid productId, string slug, int rating, string? title, string? comment)
    {
        var r = await _mediator.Send(new CreateReviewCommand(productId, rating, title, comment));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded
            ? "Thanks! Your review will appear once approved."
            : string.Join(" ", r.Errors);
        return RedirectToAction("Details", "Products", new { slug });
    }
}
