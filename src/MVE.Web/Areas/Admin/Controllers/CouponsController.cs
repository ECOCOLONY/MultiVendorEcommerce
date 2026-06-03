using Application.Coupons.Commands;
using Application.Coupons.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models.ViewModels.Coupons;

namespace Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CouponsController : Controller
{
    private readonly ISender _mediator;
    public CouponsController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(string? search = null, bool? isActive = null, int page = 1)
    {
        ViewBag.Search = search;
        ViewBag.IsActive = isActive;
        return View(await _mediator.Send(new GetPlatformCouponsQuery(search, isActive, page)));
    }

    public IActionResult Create() => View(new CouponFormViewModel { IsAdmin = true, IsActive = true });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CouponFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var r = await _mediator.Send(new CreateCouponCommand(
            vm.Code, vm.DiscountType, vm.DiscountValue, vm.MinimumOrderAmount,
            vm.MaxUses, vm.MaxUsesPerCustomer, vm.StartsAtUtc, vm.ExpiresAtUtc, vm.IsActive,
            null /* platform-wide */));

        if (!r.Succeeded) { foreach (var e in r.Errors) ModelState.AddModelError(string.Empty, e); return View(vm); }
        TempData["Success"] = "Coupon created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var c = await _mediator.Send(new GetCouponByIdQuery(id, IsAdmin: true));
        return View(new CouponFormViewModel
        {
            IsAdmin = true, Id = c.Id, Code = c.Code, DiscountType = c.DiscountType,
            DiscountValue = c.DiscountValue, MinimumOrderAmount = c.MinimumOrderAmount,
            MaxUses = c.MaxUses, MaxUsesPerCustomer = c.MaxUsesPerCustomer,
            StartsAtUtc = c.StartsAtUtc, ExpiresAtUtc = c.ExpiresAtUtc, IsActive = c.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CouponFormViewModel vm)
    {
        if (vm.Id is null) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var r = await _mediator.Send(new UpdateCouponCommand(
            vm.Id.Value, vm.DiscountValue, vm.MinimumOrderAmount,
            vm.MaxUses, vm.MaxUsesPerCustomer, vm.StartsAtUtc, vm.ExpiresAtUtc, vm.IsActive));

        if (!r.Succeeded) { foreach (var e in r.Errors) ModelState.AddModelError(string.Empty, e); return View(vm); }
        TempData["Success"] = "Coupon updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var r = await _mediator.Send(new DeleteCouponCommand(id));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Coupon deleted." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Index));
    }
}
