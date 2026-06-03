using System.ComponentModel.DataAnnotations;
using Application.Addresses.Queries.GetMyAddresses;
using Application.Cart.Queries.GetMyCart;
using Domain.Enums;

namespace Web.Models.ViewModels.Checkout;

public class CheckoutViewModel
{
    [Required, Display(Name = "Shipping address")]
    public Guid ShippingAddressId { get; set; }

    [Required, Display(Name = "Payment method")]
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Stripe;

    [StringLength(40), Display(Name = "Coupon code")]
    public string? CouponCode { get; set; }

    public CartDto? Cart { get; set; }
    public List<AddressDto> Addresses { get; set; } = new();
}
