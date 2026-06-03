using Application.AuditLogs.Queries;
using Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : Controller
{
    private readonly ISender _mediator;
    public AuditLogsController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(
        AuditAction? action = null,
        string? userSearch = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int page = 1)
    {
        ViewBag.Action = action;
        ViewBag.UserSearch = userSearch;
        ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
        ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
        return View(await _mediator.Send(new GetAuditLogsQuery(action, userSearch, dateFrom, dateTo, page)));
    }
}
