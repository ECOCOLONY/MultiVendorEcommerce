using Application.Common.Interfaces;
using Application.Reviews.Commands.CreateReview;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Moq;

namespace MVE.Application.UnitTests;

public class CreateReviewCommandHandlerTests
{
    private readonly Mock<ICurrentUserService> _user = new();
    private readonly Mock<IRealtimeNotifier> _rt = new();

    public CreateReviewCommandHandlerTests()
    {
        _user.SetupGet(u => u.UserId).Returns("user-1");
        _rt.Setup(r => r.NotifyUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                                         It.IsAny<string?>(), It.IsAny<CancellationToken>()))
           .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task CreateReview_links_OrderItemId_for_verified_purchase()
    {
        await using var db = TestDbFactory.CreateContext();

        var vendor = new Vendor { OwnerUserId = "vendor-1", BusinessName = "V", IsApproved = true };
        var store  = new VendorStore { Vendor = vendor, Name = "S", IsActive = true };
        var category = new Category { Name = "C", Slug = "c", IsActive = true };
        var product = new Product
        {
            VendorStore = store, Category = category, Name = "P", Slug = "p",
            BasePrice = 10m, IsPublished = true, ApprovalStatus = ProductApprovalStatus.Approved
        };
        var variant = new ProductVariant { Product = product, Sku = "P-1", Price = 10m, StockQuantity = 5, IsActive = true };
        var order = new Order
        {
            CustomerUserId = "user-1", OrderNumber = "ORD-1",
            Status = OrderStatus.Delivered,
            PlacedAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        var item = new OrderItem
        {
            Order = order, ProductVariant = variant, VendorStore = store,
            ProductName = "P", Quantity = 1, UnitPrice = 10m, LineTotal = 10m
        };
        order.Items.Add(item);
        db.AddRange(vendor, store, category, product, variant, order);
        await db.SaveChangesAsync();

        var handler = new CreateReviewCommandHandler(db, _user.Object, _rt.Object);

        var result = await handler.Handle(new CreateReviewCommand(product.Id, 5, "Great", "Loved it"), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        var review = db.Reviews.Single();
        review.Rating.Should().Be(5);
        review.OrderItemId.Should().Be(item.Id);   // verified-purchase link
        review.IsApproved.Should().BeFalse();      // pending moderation by default
        review.CustomerUserId.Should().Be("user-1");
    }

    [Fact]
    public async Task CreateReview_blocks_duplicate_for_same_user_and_product()
    {
        await using var db = TestDbFactory.CreateContext();

        var vendor = new Vendor { OwnerUserId = "vendor-1", BusinessName = "V", IsApproved = true };
        var store  = new VendorStore { Vendor = vendor, Name = "S", IsActive = true };
        var category = new Category { Name = "C", Slug = "c", IsActive = true };
        var product = new Product
        {
            VendorStore = store, Category = category, Name = "P", Slug = "p",
            BasePrice = 10m, IsPublished = true, ApprovalStatus = ProductApprovalStatus.Approved
        };
        db.AddRange(vendor, store, category, product);
        db.Reviews.Add(new Review { Product = product, CustomerUserId = "user-1", Rating = 4 });
        await db.SaveChangesAsync();

        var handler = new CreateReviewCommandHandler(db, _user.Object, _rt.Object);

        var result = await handler.Handle(new CreateReviewCommand(product.Id, 5, null, null), CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("already reviewed", StringComparison.OrdinalIgnoreCase));
        db.Reviews.Should().HaveCount(1);   // no second review created
    }
}
