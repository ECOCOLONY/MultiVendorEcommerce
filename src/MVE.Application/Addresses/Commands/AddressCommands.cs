using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Addresses.Commands;

// ----- CREATE -----
public sealed record CreateAddressCommand(
    string? Label, string Line1, string? Line2, string City, string? State,
    string PostalCode, string Country, string? Phone, bool IsDefault
) : IRequest<Result<Guid>>;

public sealed class CreateAddressCommandValidator : AbstractValidator<CreateAddressCommand>
{
    public CreateAddressCommandValidator()
    {
        RuleFor(x => x.Label).MaximumLength(50);
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Line2).MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).MaximumLength(50);
    }
}

public sealed class CreateAddressCommandHandler : IRequestHandler<CreateAddressCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public CreateAddressCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<Result<Guid>> Handle(CreateAddressCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var existingDefault = await _db.Addresses.AnyAsync(a => a.UserId == userId, ct);
        var entity = new Address
        {
            UserId = userId,
            Label = req.Label, Line1 = req.Line1, Line2 = req.Line2,
            City = req.City, State = req.State,
            PostalCode = req.PostalCode, Country = req.Country, Phone = req.Phone,
            IsDefault = req.IsDefault || !existingDefault
        };

        if (entity.IsDefault)
        {
            var others = await _db.Addresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync(ct);
            foreach (var o in others) o.IsDefault = false;
        }

        _db.Addresses.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Result<Guid>.Success(entity.Id);
    }
}

// ----- UPDATE -----
public sealed record UpdateAddressCommand(
    Guid Id, string? Label, string Line1, string? Line2, string City, string? State,
    string PostalCode, string Country, string? Phone, bool IsDefault
) : IRequest<Result>;

public sealed class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
    public UpdateAddressCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Line1).NotEmpty().MaximumLength(200);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PostalCode).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Country).NotEmpty().MaximumLength(100);
    }
}

public sealed class UpdateAddressCommandHandler : IRequestHandler<UpdateAddressCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public UpdateAddressCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<Result> Handle(UpdateAddressCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var entity = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == req.Id, ct);
        if (entity is null) throw new NotFoundException(nameof(Address), req.Id);
        if (entity.UserId != userId) throw new ForbiddenAccessException();

        entity.Label = req.Label;
        entity.Line1 = req.Line1; entity.Line2 = req.Line2;
        entity.City = req.City; entity.State = req.State;
        entity.PostalCode = req.PostalCode; entity.Country = req.Country;
        entity.Phone = req.Phone;

        if (req.IsDefault && !entity.IsDefault)
        {
            var others = await _db.Addresses.Where(a => a.UserId == userId && a.IsDefault).ToListAsync(ct);
            foreach (var o in others) o.IsDefault = false;
            entity.IsDefault = true;
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ----- DELETE -----
public sealed record DeleteAddressCommand(Guid Id) : IRequest<Result>;

public sealed class DeleteAddressCommandHandler : IRequestHandler<DeleteAddressCommand, Result>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _user;

    public DeleteAddressCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    {
        _db = db; _user = user;
    }

    public async Task<Result> Handle(DeleteAddressCommand req, CancellationToken ct)
    {
        var userId = _user.UserId ?? throw new ForbiddenAccessException();

        var entity = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == req.Id, ct);
        if (entity is null) throw new NotFoundException(nameof(Address), req.Id);
        if (entity.UserId != userId) throw new ForbiddenAccessException();

        _db.Addresses.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
