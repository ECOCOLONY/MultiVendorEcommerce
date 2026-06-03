using Application.AdminDashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class HomeController : Controller
{
    private readonly ISender _mediator;
    public HomeController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index() =>
        View(await _mediator.Send(new GetAdminDashboardQuery(14)));
}
