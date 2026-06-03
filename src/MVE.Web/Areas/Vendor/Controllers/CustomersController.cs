using Application.VendorOrders.Queries.GetMyCustomers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Areas.Vendor.Controllers;

[Area("Vendor")]
[Authorize(Roles = "Vendor")]
public class CustomersController : Controller
{
    private readonly ISender _mediator;
    public CustomersController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(string? search = null, int page = 1)
    {
        ViewBag.Search = search;
        return View(await _mediator.Send(new GetMyCustomersQuery(search, page)));
    }
}
