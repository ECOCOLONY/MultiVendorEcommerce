using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Products.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public DeleteProductCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<Result> Handle(DeleteProductCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var product = await _db.Products
            .Include(p => p.VendorStore).ThenInclude(s => s.Vendor)
            .FirstOrDefaultAsync(p => p.Id == req.Id, ct);

        if (product is null) throw new NotFoundException(nameof(Domain.Entities.Product), req.Id);
        if (product.VendorStore.Vendor.OwnerUserId != userId) throw new ForbiddenAccessException();

        var hasSales = await _db.OrderItems.AnyAsync(i => i.ProductVariant.ProductId == req.Id, ct);
        if (hasSales)
            return Result.Failure("This product has been ordered before and cannot be deleted. Unpublish it instead.");

        // Interceptor will convert to soft-delete.
        _db.Products.Remove(product);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
