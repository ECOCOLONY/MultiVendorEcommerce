using Application.Common.Interfaces;
using Application.Orders.Commands.PlaceOrder;
using Application.Orders.Commands.StripeCheckout;
using Application.Orders.Queries.GetCheckoutSummary;
using Application.Settings;
using Application.Settings.Queries.GetPublicWebsiteSettings;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web.Models.ViewModels.Checkout;

namespace Web.Controllers;

[Authorize]
public class CheckoutController : Controller
{
    private readonly ISender _mediator;
    private readonly IStripePaymentService _stripe;

    public CheckoutController(ISender mediator, IStripePaymentService stripe)
    {
        _mediator = mediator;
        _stripe = stripe;
    }

    public async Task<IActionResult> Index()
    {
        var summary = await _mediator.Send(new GetCheckoutSummaryQuery());
        if (!summary.Cart.Items.Any())
        {
            TempData["Error"] = "Your cart is empty.";
            return RedirectToAction("Index", "Cart");
        }
        if (!summary.Addresses.Any())
        {
            TempData["Error"] = "Please add a shipping address before checking out.";
            return RedirectToAction("Create", "Addresses");
        }

        ViewBag.AddressOptions = new SelectList(
            summary.Addresses,
            nameof(Application.Addresses.Queries.GetMyAddresses.AddressDto.Id),
            nameof(Application.Addresses.Queries.GetMyAddresses.AddressDto.Line1),
            summary.Addresses.FirstOrDefault(a => a.IsDefault)?.Id);

        ViewBag.StripeConfigured = await _stripe.IsConfiguredAsync(HttpContext.RequestAborted);

        return View(new CheckoutViewModel
        {
            Cart = summary.Cart,
            Addresses = summary.Addresses,
            ShippingAddressId = summary.Addresses.FirstOrDefault(a => a.IsDefault)?.Id ?? summary.Addresses[0].Id
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Place(CheckoutViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Please check the form and try again.";
            return RedirectToAction(nameof(Index));
        }

        if (vm.PaymentMethod == PaymentMethod.Stripe)
        {
            if (!await _stripe.IsConfiguredAsync(HttpContext.RequestAborted))
            {
                TempData["Error"] = "Stripe is not configured. Set keys under Admin → Stripe payments (or appsettings fallback).";
                return RedirectToAction(nameof(Index));
            }

            var site = await _mediator.Send(new GetPublicWebsiteSettingsQuery(), HttpContext.RequestAborted);
            var origin = PublicOrigin(Request, site);
            var successUrl = $"{origin}/Checkout/StripeReturn?session_id={{CHECKOUT_SESSION_ID}}";
            var cancelUrl = $"{origin}/Checkout";

            var r = await _mediator.Send(new CreateStripeCheckoutSessionCommand(
                vm.ShippingAddressId,
                successUrl,
                cancelUrl,
                vm.CouponCode));

            if (!r.Succeeded)
            {
                TempData["Error"] = string.Join(" ", r.Errors);
                return RedirectToAction(nameof(Index));
            }

            return Redirect(r.Value!);
        }

        var r2 = await _mediator.Send(new PlaceOrderCommand(vm.ShippingAddressId, null, vm.PaymentMethod, vm.CouponCode));
        if (!r2.Succeeded)
        {
            TempData["Error"] = string.Join(" ", r2.Errors);
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Order placed successfully.";
        return RedirectToAction("Details", "Orders", new { id = r2.Value });
    }

    /// <summary>Stripe Checkout success redirect — completes the order after payment.</summary>
    [HttpGet]
    public async Task<IActionResult> StripeReturn(string? session_id)
    {
        if (string.IsNullOrWhiteSpace(session_id))
        {
            TempData["Error"] = "Missing payment session.";
            return RedirectToAction(nameof(Index));
        }

        var r = await _mediator.Send(new CompleteStripeCheckoutCommand(session_id));
        if (!r.Succeeded)
        {
            TempData["Error"] = string.Join(" ", r.Errors);
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Order placed successfully.";
        return RedirectToAction("Details", "Orders", new { id = r.Value });
    }

    private static string PublicOrigin(HttpRequest req, PublicWebsiteSettingsDto site)
    {
        var configured = site.PublicBaseUrl?.Trim().TrimEnd('/');
        if (!string.IsNullOrEmpty(configured))
            return configured;
        return $"{req.Scheme}://{req.Host.Value}";
    }
}
