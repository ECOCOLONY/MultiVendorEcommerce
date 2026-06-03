using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Categories.Commands.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, Result>
{
    private readonly IApplicationDbContext _db;
    public DeleteCategoryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(DeleteCategoryCommand req, CancellationToken ct)
    {
        var entity = await _db.Categories
            .Include(c => c.Children)
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct);

        if (entity is null) throw new NotFoundException(nameof(Domain.Entities.Category), req.Id);

        if (entity.Children.Any())
            return Result.Failure("Cannot delete a category that has child categories. Delete or move them first.");

        var hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == req.Id, ct);
        if (hasProducts)
            return Result.Failure("Cannot delete a category that still has products assigned. Reassign or delete the products first.");

        _db.Categories.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
