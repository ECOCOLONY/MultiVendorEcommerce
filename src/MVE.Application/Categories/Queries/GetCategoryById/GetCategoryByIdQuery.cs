using Application.Common.Exceptions;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Categories.Queries.GetCategoryById;

public sealed record CategoryDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? IconUrl,
    Guid? ParentCategoryId,
    string? ParentName,
    int DisplayOrder,
    bool IsActive,
    int ProductCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public sealed record GetCategoryByIdQuery(Guid Id) : IRequest<CategoryDetailDto>;

public sealed class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDetailDto>
{
    private readonly IApplicationDbContext _db;
    public GetCategoryByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<CategoryDetailDto> Handle(GetCategoryByIdQuery req, CancellationToken ct)
    {
        var c = await _db.Categories
            .Where(x => x.Id == req.Id)
            .Select(x => new CategoryDetailDto(
                x.Id, x.Name, x.Slug, x.Description, x.IconUrl,
                x.ParentCategoryId,
                x.Parent != null ? x.Parent.Name : null,
                x.DisplayOrder, x.IsActive,
                x.Products.Count,
                x.CreatedAtUtc, x.UpdatedAtUtc))
            .FirstOrDefaultAsync(ct);

        if (c is null) throw new NotFoundException(nameof(Domain.Entities.Category), req.Id);
        return c;
    }
}
