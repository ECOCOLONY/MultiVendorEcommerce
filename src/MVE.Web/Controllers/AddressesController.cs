using Application.Addresses.Commands;
using Application.Addresses.Queries.GetMyAddressById;
using Application.Addresses.Queries.GetMyAddresses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Models.ViewModels.Addresses;

namespace Web.Controllers;

[Authorize]
public class AddressesController : Controller
{
    private readonly ISender _mediator;
    public AddressesController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index()
    {
        var list = await _mediator.Send(new GetMyAddressesQuery());
        return View(list);
    }

    public IActionResult Create() => View(new AddressFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AddressFormViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var r = await _mediator.Send(new CreateAddressCommand(
            vm.Label, vm.Line1, vm.Line2, vm.City, vm.State, vm.PostalCode, vm.Country, vm.Phone, vm.IsDefault));

        if (!r.Succeeded)
        {
            foreach (var e in r.Errors) ModelState.AddModelError(string.Empty, e);
            return View(vm);
        }

        TempData["Success"] = "Address added.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var a = await _mediator.Send(new GetMyAddressByIdQuery(id));
        return View(new AddressFormViewModel
        {
            Id = a.Id, Label = a.Label, Line1 = a.Line1, Line2 = a.Line2,
            City = a.City, State = a.State, PostalCode = a.PostalCode, Country = a.Country,
            Phone = a.Phone, IsDefault = a.IsDefault
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AddressFormViewModel vm)
    {
        if (vm.Id is null) return BadRequest();
        if (!ModelState.IsValid) return View(vm);

        var r = await _mediator.Send(new UpdateAddressCommand(
            vm.Id.Value, vm.Label, vm.Line1, vm.Line2, vm.City, vm.State,
            vm.PostalCode, vm.Country, vm.Phone, vm.IsDefault));

        if (!r.Succeeded)
        {
            foreach (var e in r.Errors) ModelState.AddModelError(string.Empty, e);
            return View(vm);
        }

        TempData["Success"] = "Address updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var r = await _mediator.Send(new DeleteAddressCommand(id));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Address deleted." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Index));
    }
}
