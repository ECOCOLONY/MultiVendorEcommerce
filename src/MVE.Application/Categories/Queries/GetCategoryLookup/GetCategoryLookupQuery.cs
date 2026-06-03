using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Categories.Queries.GetCategoryLookup;

public sealed record CategoryLookupDto(Guid Id, string Name);

public sealed record GetCategoryLookupQuery() : IRequest<List<CategoryLookupDto>>;

public sealed class GetCategoryLookupQueryHandler : IRequestHandler<GetCategoryLookupQuery, List<CategoryLookupDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCategoryLookupQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<List<CategoryLookupDto>> Handle(GetCategoryLookupQuery req, CancellationToken ct) =>
        await _db.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryLookupDto(c.Id, c.Name))
            .ToListAsync(ct);
}
