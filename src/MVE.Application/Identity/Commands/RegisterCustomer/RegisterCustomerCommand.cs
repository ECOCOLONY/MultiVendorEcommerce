using Application.Common.Interfaces;
using Application.Common.Models;
using FluentValidation;
using MediatR;

namespace Application.Identity.Commands.RegisterCustomer;

public sealed record RegisterCustomerCommand(string FullName, string Email, string Password) : IRequest<Result<string>>;

public sealed class RegisterCustomerCommandValidator : AbstractValidator<RegisterCustomerCommand>
{
    public RegisterCustomerCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().Length(2, 120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
    }
}

public sealed class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, Result<string>>
{
    private readonly IIdentityService _identity;

    public RegisterCustomerCommandHandler(IIdentityService identity) => _identity = identity;

    public Task<Result<string>> Handle(RegisterCustomerCommand req, CancellationToken ct) =>
        _identity.CreateUserAsync(req.Email, req.Password, req.FullName, "Customer");
}
