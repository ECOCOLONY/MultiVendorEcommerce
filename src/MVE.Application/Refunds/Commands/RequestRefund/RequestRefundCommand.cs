using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Refunds.Commands.RequestRefund;

public sealed record RequestRefundCommand(Guid OrderId, string Reason) : IRequest<Result>;

public sealed class RequestRefundCommandValidator : AbstractValidator<RequestRefundCommand>
{
    public RequestRefundCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().Length(10, 2000);
    }
}

public sealed class RequestRefundCommandHandler : IRequestHandler<RequestRefundCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IDateTimeService _clock;

    public RequestRefundCommandHandler(IApplicationDbContext db, ICurrentUserService user, IDateTimeService clock)
    {
        _db = db; _user = user; _clock = clock;
    }

    public async Task<Result> Handle(RequestRefundCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == req.OrderId, ct);
        if (order is null) return Result.Failure("Order not found.");
        if (order.CustomerUserId != userId) throw new ForbiddenAccessException();

        if (order.Status != OrderStatus.Delivered)
            return Result.Failure("Refund can only be requested for delivered orders.");
        if (order.RefundRequestedAtUtc.HasValue)
            return Result.Failure("A refund request is already on file for this order.");

        order.RefundRequestedAtUtc = _clock.UtcNow;
        order.RefundReason = req.Reason.Trim();

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
