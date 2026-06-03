using Application.Users.Commands.UpdateUserRoles;
using Application.Users.Queries.GetUserById;
using Application.Users.Queries.GetUsersList;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private static readonly string[] AllRoles = { "Admin", "Vendor", "Customer", "Delivery" };
    private readonly ISender _mediator;

    public UsersController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(string? search = null, string? role = null, int page = 1)
    {
        ViewBag.Search = search;
        ViewBag.Role = role;
        ViewBag.AllRoles = AllRoles;
        var list = await _mediator.Send(new GetUsersListQuery(search, role, page));
        return View(list);
    }

    public async Task<IActionResult> Details(string id)
    {
        var u = await _mediator.Send(new GetUserByIdQuery(id));
        ViewBag.AllRoles = AllRoles;
        return View(u);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRoles(string id, string[]? roles)
    {
        var r = await _mediator.Send(new UpdateUserRolesCommand(id, roles ?? Array.Empty<string>()));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Roles updated." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Details), new { id });
    }
}
