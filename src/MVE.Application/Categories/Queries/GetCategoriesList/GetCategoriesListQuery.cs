using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Categories.Queries.GetCategoriesList;

public sealed record CategoryListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? IconUrl,
    Guid? ParentCategoryId,
    string? ParentName,
    int DisplayOrder,
    bool IsActive,
    int ProductCount
);

public sealed record GetCategoriesListQuery() : IRequest<List<CategoryListItemDto>>;

public sealed class GetCategoriesListQueryHandler : IRequestHandler<GetCategoriesListQuery, List<CategoryListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCategoriesListQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CategoryListItemDto>> Handle(GetCategoriesListQuery req, CancellationToken ct) =>
        await _db.Categories
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryListItemDto(
                c.Id, c.Name, c.Slug, c.Description, c.IconUrl,
                c.ParentCategoryId,
                c.Parent != null ? c.Parent.Name : null,
                c.DisplayOrder, c.IsActive,
                c.Products.Count))
            .ToListAsync(ct);
}
