using Application.Common.Interfaces;
using Application.Profile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models.ViewModels.Profile;

namespace Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ISender _mediator;
    private readonly IFileStorageService _files;

    public ProfileController(ISender mediator, IFileStorageService files)
    {
        _mediator = mediator;
        _files = files;
    }

    public async Task<IActionResult> Index()
    {
        var p = await _mediator.Send(new GetMyProfileQuery());
        var vm = new ProfileViewModel
        {
            Email = p.Email,
            FullName = p.FullName,
            ProfileImageUrl = p.ProfileImageUrl,
            Roles = p.Roles
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ProfileViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        string? imageUrl = vm.ProfileImageUrl;
        if (vm.ProfileImageFile is { Length: > 0 })
            imageUrl = await _files.SaveAsync(vm.ProfileImageFile, "users");

        var r = await _mediator.Send(new UpdateProfileCommand(vm.FullName, imageUrl));
        if (!r.Succeeded)
        {
            foreach (var e in r.Errors) ModelState.AddModelError(string.Empty, e);
            return View(vm);
        }

        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        var r = await _mediator.Send(new ChangePasswordCommand(vm.CurrentPassword, vm.NewPassword));
        if (!r.Succeeded)
        {
            foreach (var e in r.Errors) ModelState.AddModelError(string.Empty, e);
            return View(vm);
        }
        TempData["Success"] = "Password changed.";
        return RedirectToAction(nameof(Index));
    }
}
