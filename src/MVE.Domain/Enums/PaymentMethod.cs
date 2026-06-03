namespace Domain.Enums;

public enum PaymentMethod
{
    /// <summary>Simulated immediate capture (no gateway).</summary>
    CreditCard = 0,

    BankTransfer = 1,

    CashOnDelivery = 2,

    /// <summary>Card payment via Stripe Checkout.</summary>
    Stripe = 3
}
