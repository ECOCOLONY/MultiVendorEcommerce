using Application.Addresses.Queries.GetMyAddresses;
using Application.Cart.Queries.GetMyCart;
using MediatR;

namespace Application.Orders.Queries.GetCheckoutSummary;

public sealed record CheckoutSummaryDto(CartDto Cart, List<AddressDto> Addresses);

public sealed record GetCheckoutSummaryQuery() : IRequest<CheckoutSummaryDto>;

public sealed class GetCheckoutSummaryQueryHandler : IRequestHandler<GetCheckoutSummaryQuery, CheckoutSummaryDto>
{
    private readonly ISender _mediator;
    public GetCheckoutSummaryQueryHandler(ISender mediator) => _mediator = mediator;

    public async Task<CheckoutSummaryDto> Handle(GetCheckoutSummaryQuery req, CancellationToken ct)
    {
        var cart      = await _mediator.Send(new GetMyCartQuery(), ct);
        var addresses = await _mediator.Send(new GetMyAddressesQuery(), ct);
        return new CheckoutSummaryDto(cart, addresses);
    }
}
