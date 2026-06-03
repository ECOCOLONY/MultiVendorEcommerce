using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Reviews.Commands.CreateReview;

public sealed record CreateReviewCommand(
    Guid ProductId,
    int Rating,
    string? Title,
    string? Comment
) : IRequest<Result<Guid>>;

public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    public CreateReviewCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Title).MaximumLength(200);
        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}

public sealed class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;
    private readonly IRealtimeNotifier _rt;

    public CreateReviewCommandHandler(IApplicationDbContext db, ICurrentUserService user, IRealtimeNotifier rt)
    { _db = db; _user = user; _rt = rt; }

    public async Task<Result<Guid>> Handle(CreateReviewCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var alreadyReviewed = await _db.Reviews
            .AnyAsync(r => r.ProductId == req.ProductId && r.CustomerUserId == userId, ct);
        if (alreadyReviewed)
            return Result<Guid>.Failure("You have already reviewed this product.");

        // Find the most recent OrderItem this user has of this product (verified-purchase link).
        var orderItemId = await _db.OrderItems
            .Where(i => i.Order.CustomerUserId == userId && i.ProductVariant.ProductId == req.ProductId)
            .OrderByDescending(i => i.Order.PlacedAtUtc)
            .Select(i => (Guid?)i.Id)
            .FirstOrDefaultAsync(ct);

        var review = new Review
        {
            ProductId = req.ProductId,
            CustomerUserId = userId,
            OrderItemId = orderItemId,
            Rating = req.Rating,
            Title = req.Title?.Trim(),
            Comment = req.Comment?.Trim(),
            IsApproved = false
        };
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync(ct);

        // Notify the vendor.
        var ownerId = await _db.Products
            .Where(p => p.Id == req.ProductId)
            .Select(p => p.VendorStore.Vendor.OwnerUserId)
            .FirstOrDefaultAsync(ct);
        if (!string.IsNullOrEmpty(ownerId))
        {
            var title = "New review submitted";
            var body  = $"A customer left a {req.Rating}-star review on one of your products.";
            const string url = "/Vendor/Reviews";

            _db.Notifications.Add(new Notification
            {
                UserId = ownerId,
                Title = title, Body = body,
                Type = Domain.Enums.NotificationType.System,
                ActionUrl = url
            });
            await _db.SaveChangesAsync(ct);
            await _rt.NotifyUserAsync(ownerId, title, body, url, ct);
        }

        return Result<Guid>.Success(review.Id);
    }
}
