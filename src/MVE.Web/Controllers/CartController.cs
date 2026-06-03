using Application.Cart.Commands.AddToCart;
using Application.Cart.Commands.RemoveCartItem;
using Application.Cart.Commands.UpdateCartItem;
using Application.Cart.Queries.GetMyCart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly ISender _mediator;
    public CartController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index()
    {
        var cart = await _mediator.Send(new GetMyCartQuery());
        return View(cart);
    }

    // Catches the GET redirect that happens after login when returnUrl was /Cart/Add
    [HttpGet]
    public IActionResult Add()
    {
        TempData["Info"] = "You're now signed in — please add the item to your cart again.";
        return RedirectToAction("Index", "Products");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid productVariantId, int quantity = 1)
    {
        var r = await _mediator.Send(new AddToCartCommand(productVariantId, quantity));
        if (r.Succeeded) TempData["Success"] = "Added to cart.";
        else TempData["Error"] = string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(Guid id, int quantity)
    {
        var r = await _mediator.Send(new UpdateCartItemCommand(id, quantity));
        if (!r.Succeeded) TempData["Error"] = string.Join(" ", r.Errors);
        else TempData["Success"] = quantity == 0 ? "Item removed." : "Cart updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid id)
    {
        var r = await _mediator.Send(new RemoveCartItemCommand(id));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Item removed." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Index));
    }
}
