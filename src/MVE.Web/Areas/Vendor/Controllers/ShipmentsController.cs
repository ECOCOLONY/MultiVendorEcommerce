using Application.Shipments.Commands.MarkShipmentDelivered;
using Application.Shipments.Queries.GetMyShipments;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Areas.Vendor.Controllers;

[Area("Vendor")]
[Authorize(Roles = "Vendor")]
public class ShipmentsController : Controller
{
    private readonly ISender _mediator;
    public ShipmentsController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(ShipmentStatus? status = null, string? search = null, int page = 1)
    {
        ViewBag.Status = status;
        ViewBag.Search = search;
        var list = await _mediator.Send(new GetMyShipmentsQuery(status, search, page));
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDelivered(Guid id)
    {
        var r = await _mediator.Send(new MarkShipmentDeliveredCommand(id));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Shipment marked delivered." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Index));
    }
}
