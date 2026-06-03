using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Vendors.Commands.SetVendorApproval;

public sealed record SetVendorApprovalCommand(Guid VendorId, bool IsApproved) : IRequest<Result>;

public sealed class SetVendorApprovalCommandHandler : IRequestHandler<SetVendorApprovalCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;

    public SetVendorApprovalCommandHandler(IApplicationDbContext db, IAuditLogger audit)
    { _db = db; _audit = audit; }

    public async Task<Result> Handle(SetVendorApprovalCommand req, CancellationToken ct)
    {
        var vendor = await _db.Vendors
            .Include(v => v.Stores)
            .FirstOrDefaultAsync(v => v.Id == req.VendorId, ct);

        if (vendor is null) throw new NotFoundException(nameof(Domain.Entities.Vendor), req.VendorId);

        vendor.IsApproved = req.IsApproved;
        if (req.IsApproved)
            foreach (var s in vendor.Stores) s.IsActive = true;

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(
            AuditAction.Update,
            entityName: "Vendor",
            entityId: vendor.Id.ToString(),
            newValuesJson: $"{{\"isApproved\":{(req.IsApproved ? "true" : "false")}}}",
            ct: ct);

        return Result.Success();
    }
}
