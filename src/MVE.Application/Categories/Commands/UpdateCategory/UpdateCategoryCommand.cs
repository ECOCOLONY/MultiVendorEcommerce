using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Categories.Commands.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid Id,
    string Name,
    string? Description,
    string? IconUrl,
    Guid? ParentCategoryId,
    int DisplayOrder,
    bool IsActive
) : IRequest<Result>;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().Length(2, 120);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.IconUrl).MaximumLength(500);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => x.ParentCategoryId != x.Id)
            .WithMessage("A category cannot be its own parent.");
    }
}

public sealed class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, Result>
{
    private readonly IApplicationDbContext _db;
    public UpdateCategoryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateCategoryCommand req, CancellationToken ct)
    {
        var entity = await _db.Categories.FirstOrDefaultAsync(c => c.Id == req.Id, ct);
        if (entity is null) throw new NotFoundException(nameof(Domain.Entities.Category), req.Id);

        entity.Name = req.Name.Trim();
        entity.Description = req.Description?.Trim();
        entity.IconUrl = req.IconUrl?.Trim();
        entity.ParentCategoryId = req.ParentCategoryId;
        entity.DisplayOrder = req.DisplayOrder;
        entity.IsActive = req.IsActive;

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
