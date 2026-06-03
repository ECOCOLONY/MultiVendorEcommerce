using Application.Common.Interfaces;
using Application.VendorStores.Commands.UpdateMyStore;
using Application.VendorStores.Queries.GetMyStore;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models.ViewModels.Vendor.Settings;

namespace Web.Areas.Vendor.Controllers;

[Area("Vendor")]
[Authorize(Roles = "Vendor")]
public class SettingsController : Controller
{
    private readonly ISender _mediator;
    private readonly IFileStorageService _files;

    public SettingsController(ISender mediator, IFileStorageService files)
    {
        _mediator = mediator;
        _files = files;
    }

    public async Task<IActionResult> Index()
    {
        var store = await _mediator.Send(new GetMyStoreQuery());
        var vm = new StoreSettingsViewModel
        {
            StoreId = store.Id,
            Name = store.Name,
            Slug = store.Slug,
            Description = store.Description,
            ContactEmail = store.ContactEmail,
            ContactPhone = store.ContactPhone,
            LogoUrl = store.LogoUrl,
            BannerUrl = store.BannerUrl
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(StoreSettingsViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        string? logoUrl = vm.LogoUrl;
        if (vm.LogoFile is { Length: > 0 })
            logoUrl = await _files.SaveAsync(vm.LogoFile, "stores/logos");

        string? bannerUrl = vm.BannerUrl;
        if (vm.BannerFile is { Length: > 0 })
            bannerUrl = await _files.SaveAsync(vm.BannerFile, "stores/banners");

        var result = await _mediator.Send(new UpdateMyStoreCommand(
            vm.StoreId, vm.Name, vm.Description, vm.ContactEmail, vm.ContactPhone, logoUrl, bannerUrl));

        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);
            return View(vm);
        }

        TempData["Success"] = "Store settings saved.";
        return RedirectToAction(nameof(Index));
    }
}
