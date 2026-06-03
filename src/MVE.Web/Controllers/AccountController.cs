using Application.Common.Interfaces;
using Application.Identity.Commands.RegisterCustomer;
using Application.Identity.Commands.RegisterVendor;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models.ViewModels.Account;

namespace Web.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IIdentityService _identity;
    private readonly ISender _mediator;
    private readonly IAuditLogger _audit;

    public AccountController(IIdentityService identity, ISender mediator, IAuditLogger audit)
    {
        _identity = identity;
        _mediator = mediator;
        _audit = audit;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _identity.SignInAsync(vm.Email, vm.Password, vm.RememberMe);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);
            return View(vm);
        }

        await _audit.LogAsync(AuditAction.Login, "User", entityId: vm.Email);

        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
            return Redirect(vm.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _mediator.Send(new RegisterCustomerCommand(vm.FullName, vm.Email, vm.Password));
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);
            return View(vm);
        }

        await _identity.SignInAsync(vm.Email, vm.Password, false);
        TempData["Success"] = "Welcome! Your account has been created.";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult RegisterVendor() => View(new RegisterVendorViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegisterVendor(RegisterVendorViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _mediator.Send(new RegisterVendorCommand(
            vm.FullName, vm.Email, vm.Password, vm.BusinessName, vm.StoreName, vm.TaxNumber));

        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);
            return View(vm);
        }

        TempData["Success"] = "Vendor account created. An admin will review and approve your store.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Logout()
    {
        await _audit.LogAsync(AuditAction.Logout, "User");
        await _identity.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
