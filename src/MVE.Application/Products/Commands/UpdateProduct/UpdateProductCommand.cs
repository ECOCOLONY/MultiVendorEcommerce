using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Description,
    decimal BasePrice,
    bool IsPublished
) : IRequest<Result>;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().Length(2, 200);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.BasePrice).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public UpdateProductCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<Result> Handle(UpdateProductCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var product = await _db.Products
            .Include(p => p.VendorStore).ThenInclude(s => s.Vendor)
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct);

        if (product is null) throw new NotFoundException(nameof(Domain.Entities.Product), req.Id);
        if (product.VendorStore.Vendor.OwnerUserId != userId) throw new ForbiddenAccessException();

        product.CategoryId  = req.CategoryId;
        product.Name        = req.Name.Trim();
        product.Description = req.Description?.Trim();
        product.BasePrice   = req.BasePrice;
        product.IsPublished = req.IsPublished;

        // Edits reset the approval gate — admin re-reviews.
        if (product.ApprovalStatus == ProductApprovalStatus.Approved)
            product.ApprovalStatus = ProductApprovalStatus.Pending;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
