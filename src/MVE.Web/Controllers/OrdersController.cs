using Application.Orders.Commands.CancelOrder;
using Application.Orders.Queries.GetMyOrderById;
using Application.Orders.Queries.GetMyOrders;
using Application.Refunds.Commands.RequestRefund;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Services;

namespace Web.Controllers;

[Authorize]
public class OrdersController : Controller
{
    private readonly ISender _mediator;
    public OrdersController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(int page = 1)
    {
        var list = await _mediator.Send(new GetMyOrdersQuery(page, 10));
        return View(list);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var o = await _mediator.Send(new GetMyOrderByIdQuery(id));
        return View(o);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var r = await _mediator.Send(new CancelOrderCommand(id));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Order cancelled and refunded." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Invoice(Guid id)
    {
        var o = await _mediator.Send(new GetMyOrderByIdQuery(id));
        var pdf = OrderInvoicePdf.Generate(o);
        return File(pdf, "application/pdf", $"{o.OrderNumber}.pdf");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestRefund(Guid id, string reason)
    {
        var r = await _mediator.Send(new RequestRefundCommand(id, reason ?? string.Empty));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Refund request submitted." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Details), new { id });
    }
}
