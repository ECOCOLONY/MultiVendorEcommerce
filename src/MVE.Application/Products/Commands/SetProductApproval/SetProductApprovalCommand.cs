using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Products.Commands.SetProductApproval;

public sealed record SetProductApprovalCommand(Guid ProductId, ProductApprovalStatus Status) : IRequest<Result>;

public sealed class SetProductApprovalCommandHandler : IRequestHandler<SetProductApprovalCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    public SetProductApprovalCommandHandler(IApplicationDbContext db, IAuditLogger audit)
    { _db = db; _audit = audit; }

    public async Task<Result> Handle(SetProductApprovalCommand req, CancellationToken ct)
    {
        var p = await _db.Products.FirstOrDefaultAsync(x => x.Id == req.ProductId, ct);
        if (p is null) throw new NotFoundException(nameof(Domain.Entities.Product), req.ProductId);

        p.ApprovalStatus = req.Status;
        if (req.Status == ProductApprovalStatus.Approved) p.IsPublished = true;
        else if (req.Status == ProductApprovalStatus.Rejected) p.IsPublished = false;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            AuditAction.Update,
            entityName: "Product",
            entityId: p.Id.ToString(),
            newValuesJson: $"{{\"approvalStatus\":\"{req.Status}\"}}",
            ct: ct);

        return Result.Success();
    }
}
