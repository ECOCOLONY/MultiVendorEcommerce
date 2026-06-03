using Application.Common.Interfaces;
using Application.Orders.Commands.PlaceOrder;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace MVE.Application.UnitTests;

public class PlaceOrderCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _user = new();
    private readonly Mock<IDateTimeService> _clock = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<IIdentityService> _identity = new();

    public PlaceOrderCommandHandlerTests()
    {
        _user.SetupGet(u => u.UserId).Returns("user-1");
        _clock.SetupGet(c => c.UtcNow).Returns(new DateTime(2026, 5, 3, 12, 0, 0, DateTimeKind.Utc));
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .Returns(Task.CompletedTask);
        _identity.Setup(i => i.GetUserAsync("user-1"))
                 .ReturnsAsync(new UserSummary("user-1", "buyer@example.com", "Buyer", DateTime.UtcNow, new[] { "Customer" }));
    }

    [Fact]
    public async Task PlaceOrder_snapshots_commission_decrements_stock_and_clears_cart()
    {
        await using var db = TestDbFactory.CreateContext();

        // ---- arrange: vendor + store + category + product + variant + cart + address
        var vendor = new Vendor { OwnerUserId = "vendor-1", BusinessName = "Acme", IsApproved = true, DefaultCommissionPercent = 10m };
        var store  = new VendorStore { Vendor = vendor, Name = "Acme Store", IsActive = true };
        var category = new Category { Name = "Cat", Slug = "cat", IsActive = true };
        var product  = new Product
        {
            VendorStore = store, Category = category,
            Name = "Widget", Slug = "widget",
            BasePrice = 100m, IsPublished = true, ApprovalStatus = ProductApprovalStatus.Approved
        };
        var variant = new ProductVariant
        {
            Product = product, Sku = "WGT-1",
            Price = 100m, StockQuantity = 5, IsActive = true
        };
        var address = new Address
        {
            UserId = "user-1", Line1 = "1 Main St", City = "Town", PostalCode = "00000", Country = "USA"
        };
        var cart = new Cart { CustomerUserId = "user-1" };
        cart.Items.Add(new CartItem { ProductVariant = variant, Quantity = 2, UnitPrice = 100m });

        db.AddRange(vendor, store, category, product, variant, address, cart);
        await db.SaveChangesAsync();

        var handler = new PlaceOrderCommandHandler(db, _user.Object, _clock.Object, _email.Object, _identity.Object);
        var command = new PlaceOrderCommand(address.Id, null, PaymentMethod.CreditCard);

        // ---- act
        var result = await handler.Handle(command, CancellationToken.None);

        // ---- assert
        result.Succeeded.Should().BeTrue(string.Join("; ", result.Errors));

        var order = db.Orders.Single();
        order.Status.Should().Be(OrderStatus.Paid);
        order.PlacedAtUtc.Should().Be(_clock.Object.UtcNow);
        order.Subtotal.Should().Be(200m);   // 100 * 2
        order.Total.Should().Be(200m);

        var item = order.Items.Single();
        item.Quantity.Should().Be(2);
        item.UnitPrice.Should().Be(100m);
        item.LineTotal.Should().Be(200m);
        item.CommissionPercent.Should().Be(10m);
        item.CommissionAmount.Should().Be(20m);    // 200 * 10%
        item.VendorNetAmount.Should().Be(180m);    // 200 - 20

        // Stock decremented
        var freshVariant = db.ProductVariants.Single();
        freshVariant.StockQuantity.Should().Be(3);

        // Cart cleared
        db.CartItems.Should().BeEmpty();

        // One captured payment recorded
        order.Payments.Single().Status.Should().Be(PaymentStatus.Captured);
        order.Payments.Single().Amount.Should().Be(200m);
    }

    [Fact]
    public async Task PlaceOrder_rejects_when_cart_is_empty()
    {
        await using var db = TestDbFactory.CreateContext();
        db.Carts.Add(new Cart { CustomerUserId = "user-1" });
        await db.SaveChangesAsync();

        var handler = new PlaceOrderCommandHandler(db, _user.Object, _clock.Object, _email.Object, _identity.Object);
        var address = new Address { UserId = "user-1", Line1 = "x", City = "x", PostalCode = "x", Country = "x" };
        db.Addresses.Add(address);
        await db.SaveChangesAsync();

        var result = await handler.Handle(new PlaceOrderCommand(address.Id, null, PaymentMethod.CreditCard), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("cart is empty", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PlaceOrder_rejects_when_stock_insufficient()
    {
        await using var db = TestDbFactory.CreateContext();
        var vendor = new Vendor { OwnerUserId = "v", BusinessName = "V", IsApproved = true, DefaultCommissionPercent = 0m };
        var store  = new VendorStore { Vendor = vendor, Name = "S", IsActive = true };
        var category = new Category { Name = "C", Slug = "c", IsActive = true };
        var product  = new Product
        {
            VendorStore = store, Category = category, Name = "P", Slug = "p",
            BasePrice = 10m, IsPublished = true, ApprovalStatus = ProductApprovalStatus.Approved
        };
        var variant = new ProductVariant { Product = product, Sku = "P-1", Price = 10m, StockQuantity = 1, IsActive = true };
        var address = new Address { UserId = "user-1", Line1 = "x", City = "x", PostalCode = "x", Country = "x" };
        var cart = new Cart { CustomerUserId = "user-1" };
        cart.Items.Add(new CartItem { ProductVariant = variant, Quantity = 5, UnitPrice = 10m });
        db.AddRange(vendor, store, category, product, variant, address, cart);
        await db.SaveChangesAsync();

        var handler = new PlaceOrderCommandHandler(db, _user.Object, _clock.Object, _email.Object, _identity.Object);
        var result  = await handler.Handle(new PlaceOrderCommand(address.Id, null, PaymentMethod.CreditCard), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("Not enough stock", StringComparison.OrdinalIgnoreCase));
    }
}
