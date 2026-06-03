using Application.Common.Exceptions;
using Application.Settings.Commands.UpdateStripeSettings;
using Application.Settings.Queries.GetStripeSettingsForAdmin;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models.ViewModels.Admin;

namespace Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class StripeSettingsController : Controller
{
    private readonly ISender _mediator;

    public StripeSettingsController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var dto = await _mediator.Send(new GetStripeSettingsForAdminQuery(), ct);
        var vm = new StripeSettingsFormViewModel
        {
            PublishableKey = dto.PublishableKey,
            Currency = dto.Currency,
            WebhookSecret = dto.WebhookSecret,
            HasSecretKey = dto.HasSecretKey
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(StripeSettingsFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return View(vm);

        try
        {
            await _mediator.Send(new UpdateStripeSettingsCommand(
                string.IsNullOrWhiteSpace(vm.NewSecretKey) ? null : vm.NewSecretKey.Trim(),
                vm.PublishableKey ?? "",
                vm.Currency ?? "usd",
                vm.WebhookSecret), ct);
        }
        catch (ValidationException vex)
        {
            foreach (var kv in vex.Errors)
                foreach (var msg in kv.Value)
                    ModelState.AddModelError(kv.Key, msg);
            return View(vm);
        }

        TempData["Success"] = "Stripe settings saved.";
        return RedirectToAction(nameof(Index));
    }
}
