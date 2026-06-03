using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Reviews.Commands;

// ----- Admin: approve / reject -----
public sealed record SetReviewApprovalCommand(Guid ReviewId, bool IsApproved) : IRequest<Result>;

public sealed class SetReviewApprovalCommandHandler : IRequestHandler<SetReviewApprovalCommand, Result>
{
    private readonly IApplicationDbContext _db;
    public SetReviewApprovalCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(SetReviewApprovalCommand req, CancellationToken ct)
    {
        var r = await _db.Reviews
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == req.ReviewId, ct);
        if (r is null) return Result.Failure("Review not found.");

        r.IsApproved = req.IsApproved;
        await _db.SaveChangesAsync(ct);

        // Recompute denormalized rating fields on the product.
        var stats = await _db.Reviews
            .Where(rv => rv.ProductId == r.ProductId && rv.IsApproved)
            .GroupBy(rv => 1)
            .Select(g => new { Avg = (decimal)g.Average(rv => rv.Rating), Count = g.Count() })
            .FirstOrDefaultAsync(ct);
        r.Product.AverageRating = stats?.Avg ?? 0m;
        r.Product.ReviewCount = stats?.Count ?? 0;
        await _db.SaveChangesAsync(ct);

        return Result.Success();
    }
}

// ----- Vendor: reply -----
public sealed record ReplyReviewCommand(Guid ReviewId, string Reply) : IRequest<Result>;

public sealed class ReplyReviewCommandValidator : AbstractValidator<ReplyReviewCommand>
{
    public ReplyReviewCommandValidator()
    {
        RuleFor(x => x.Reply).NotEmpty().MaximumLength(2000);
    }
}

public sealed class ReplyReviewCommandHandler : IRequestHandler<ReplyReviewCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IDateTimeService _clock;

    public ReplyReviewCommandHandler(IApplicationDbContext db, ICurrentUserService user, IDateTimeService clock)
    { _db = db; _user = user; _clock = clock; }

    public async Task<Result> Handle(ReplyReviewCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();
        var review = await _db.Reviews
            .Include(r => r.Product).ThenInclude(p => p.VendorStore).ThenInclude(s => s.Vendor)
            .FirstOrDefaultAsync(r => r.Id == req.ReviewId, ct);
        if (review is null) return Result.Failure("Review not found.");
        if (review.Product.VendorStore.Vendor.OwnerUserId != userId) throw new ForbiddenAccessException();

        review.VendorReply = req.Reply.Trim();
        review.VendorRepliedAtUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
