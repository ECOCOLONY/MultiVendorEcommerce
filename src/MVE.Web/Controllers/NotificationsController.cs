using Application.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ISender _mediator;
    public NotificationsController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index() =>
        View(await _mediator.Send(new GetMyNotificationsQuery()));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await _mediator.Send(new MarkNotificationReadCommand(id));
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        await _mediator.Send(new MarkAllNotificationsReadCommand());
        return RedirectToAction(nameof(Index));
    }
}
