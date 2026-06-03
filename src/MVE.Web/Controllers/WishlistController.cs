using Application.Wishlist;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly ISender _mediator;
    public WishlistController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index() =>
        View(await _mediator.Send(new GetMyWishlistQuery()));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid productId, string? slug = null)
    {
        var r = await _mediator.Send(new AddToWishlistCommand(productId));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Added to wishlist." : string.Join(" ", r.Errors);
        return slug is null
            ? RedirectToAction(nameof(Index))
            : RedirectToAction("Details", "Products", new { slug });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid id)
    {
        var r = await _mediator.Send(new RemoveFromWishlistCommand(id));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Removed from wishlist." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Index));
    }
}
