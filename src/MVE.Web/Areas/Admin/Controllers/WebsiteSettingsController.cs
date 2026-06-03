using Application.Settings;
using Application.Settings.Commands.UpdateWebsiteSettings;
using Application.Settings.Queries.GetWebsiteSettingsForAdmin;
using Application.Common.Interfaces;
using Application.Common.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class WebsiteSettingsController : Controller
{
    private readonly ISender _mediator;
    private readonly IFileStorageService _files;

    public WebsiteSettingsController(ISender mediator, IFileStorageService files)
    {
        _mediator = mediator;
        _files = files;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var model = await _mediator.Send(new GetWebsiteSettingsForAdminQuery(), ct);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PublicWebsiteSettingsDto model,
        IFormFile? headerLogoFile,
        IFormFile? footerLogoFile,
        IFormFile? faviconFile,
        CancellationToken ct)
    {
        if (headerLogoFile is { Length: > 0 })
            model.HeaderLogoUrl = await _files.SaveAsync(headerLogoFile, "site", ct);
        if (footerLogoFile is { Length: > 0 })
            model.FooterLogoUrl = await _files.SaveAsync(footerLogoFile, "site", ct);
        if (faviconFile is { Length: > 0 })
            model.FaviconUrl = await _files.SaveAsync(faviconFile, "site", ct);

        try
        {
            await _mediator.Send(new UpdateWebsiteSettingsCommand(model), ct);
        }
        catch (ValidationException vex)
        {
            foreach (var kv in vex.Errors)
                foreach (var msg in kv.Value)
                    ModelState.AddModelError(kv.Key, msg);
            return View(model);
        }

        TempData["Success"] = "Website settings saved.";
        return RedirectToAction(nameof(Index));
    }
}
