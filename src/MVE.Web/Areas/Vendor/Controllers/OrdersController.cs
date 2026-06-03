using Application.Shipments.Commands.CreateShipment;
using Application.VendorOrders.Commands.UpdateItemStatus;
using Application.VendorOrders.Queries.GetVendorOrderById;
using Application.VendorOrders.Queries.GetVendorOrders;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Areas.Vendor.Controllers;

[Area("Vendor")]
[Authorize(Roles = "Vendor")]
public class OrdersController : Controller
{
    private readonly ISender _mediator;
    public OrdersController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(
        OrderStatus? status = null,
        string? search = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int page = 1)
    {
        ViewBag.Status = status;
        ViewBag.Search = search;
        ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
        ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
        var list = await _mediator.Send(new GetVendorOrdersQuery(status, search, dateFrom, dateTo, page));
        return View(list);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var detail = await _mediator.Send(new GetVendorOrderByIdQuery(id));
        return View(detail);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetItemStatus(Guid id, Guid orderItemId, VendorOrderItemStatus status)
    {
        var r = await _mediator.Send(new UpdateVendorItemStatusCommand(orderItemId, status));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Item status updated." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateShipment(Guid id, string carrier, string trackingNumber, DateTime? estimatedDeliveryAtUtc)
    {
        var r = await _mediator.Send(new CreateShipmentCommand(id, carrier, trackingNumber, estimatedDeliveryAtUtc));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Shipment created. Items marked as Shipped." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Details), new { id });
    }
}
