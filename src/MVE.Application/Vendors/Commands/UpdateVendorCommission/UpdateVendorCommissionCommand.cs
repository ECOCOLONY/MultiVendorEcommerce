using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Vendors.Commands.UpdateVendorCommission;

public sealed record UpdateVendorCommissionCommand(Guid VendorId, decimal CommissionPercent) : IRequest<Result>;

public sealed class UpdateVendorCommissionCommandValidator : AbstractValidator<UpdateVendorCommissionCommand>
{
    public UpdateVendorCommissionCommandValidator()
    {
        RuleFor(x => x.CommissionPercent).InclusiveBetween(0m, 100m);
    }
}

public sealed class UpdateVendorCommissionCommandHandler : IRequestHandler<UpdateVendorCommissionCommand, Result>
{
    private readonly IApplicationDbContext _db;
    public UpdateVendorCommissionCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(UpdateVendorCommissionCommand req, CancellationToken ct)
    {
        var vendor = await _db.Vendors.FirstOrDefaultAsync(v => v.Id == req.VendorId, ct);
        if (vendor is null) throw new NotFoundException(nameof(Domain.Entities.Vendor), req.VendorId);

        vendor.DefaultCommissionPercent = req.CommissionPercent;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
