using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string? Description,
    string? IconUrl,
    Guid? ParentCategoryId,
    int DisplayOrder,
    bool IsActive
) : IRequest<Result<Guid>>;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().Length(2, 120);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.IconUrl).MaximumLength(500);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    public CreateCategoryCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<Guid>> Handle(CreateCategoryCommand req, CancellationToken ct)
    {
        var slug = SlugMaker.Make(req.Name);
        if (await _db.Categories.AnyAsync(c => c.Slug == slug, ct))
            return Result<Guid>.Failure($"A category with slug '{slug}' already exists.");

        var entity = new Category
        {
            Name = req.Name.Trim(),
            Slug = slug,
            Description = req.Description?.Trim(),
            IconUrl = req.IconUrl?.Trim(),
            ParentCategoryId = req.ParentCategoryId,
            DisplayOrder = req.DisplayOrder,
            IsActive = req.IsActive
        };
        _db.Categories.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(entity.Id);
    }
}

internal static class SlugMaker
{
    public static string Make(string input)
    {
        var s = input.Trim().ToLowerInvariant();
        var chars = s.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var collapsed = new string(chars);
        while (collapsed.Contains("--")) collapsed = collapsed.Replace("--", "-");
        return collapsed.Trim('-');
    }
}
