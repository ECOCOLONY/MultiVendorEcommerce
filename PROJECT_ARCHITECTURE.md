# MVE â€” Full Architecture Reference

> ASP.NET Core 8 MVC Â· C# 12 Â· Clean Architecture Â· EF Core 8 on SQL Server Â· MediatR Â· FluentValidation Â· ASP.NET Core Identity Â· SignalR Â· QuestPDF Â· Bootstrap 5 Â· **AdminLTE 4**

---

## Table of Contents

1. [Solution Structure](#1-solution-structure)
2. [Architecture Overview](#2-architecture-overview)
3. [Domain Layer](#3-domain-layer)
4. [Application Layer](#4-application-layer)
5. [Infrastructure Layer](#5-infrastructure-layer)
6. [Web Layer](#6-web-layer)
7. [Key Cross-Cutting Patterns](#7-key-cross-cutting-patterns)
8. [Data Flow: Place an Order](#8-data-flow-place-an-order)
9. [Default Credentials & Seed Data](#9-default-credentials--seed-data)
10. [UI Template â€” AdminLTE 4](#10-ui-template--adminlte-4)
11. [Feature Catalogue by Role](#11-feature-catalogue-by-role)
    - [Customer Features](#111-customer-features)
    - [Vendor Features](#112-vendor-features)
    - [Admin Features](#113-admin-features)
3. [Domain Layer](#3-domain-layer)
   - [Base Classes](#31-base-classes)
   - [Entities](#32-entities)
   - [Enumerations](#33-enumerations)
   - [Domain Events](#34-domain-events)
4. [Application Layer](#4-application-layer)
   - [Common Infrastructure](#41-common-infrastructure)
   - [Interfaces](#42-interfaces)
   - [MediatR Pipeline Behaviours](#43-mediatr-pipeline-behaviours)
   - [Feature Folders](#44-feature-folders)
5. [Infrastructure Layer](#5-infrastructure-layer)
   - [Identity](#51-identity)
   - [DbContext](#52-dbcontext)
   - [EF Core Configurations](#53-ef-core-configurations)
   - [EF Core Interceptors](#54-ef-core-interceptors)
   - [Service Implementations](#55-service-implementations)
   - [Database Seeding](#56-database-seeding)
6. [Web Layer](#6-web-layer)
   - [Startup & Middleware](#61-startup--middleware)
   - [Admin Area](#62-admin-area)
   - [Vendor Area](#63-vendor-area)
   - [Customer-Facing Controllers](#64-customer-facing-controllers)
   - [Real-Time Notifications](#65-real-time-notifications)
   - [Views & Shared Components](#66-views--shared-components)
7. [Key Cross-Cutting Patterns](#7-key-cross-cutting-patterns)
8. [Data Flow: Place an Order](#8-data-flow-place-an-order)
9. [Default Credentials & Seed Data](#9-default-credentials--seed-data)

---

## 1. Solution Structure

```
MVE.sln
â”œâ”€â”€ src/
â”‚   â”œâ”€â”€ MVE.Domain          # Pure C#, no framework dependencies
â”‚   â”œâ”€â”€ MVE.Application     # CQRS handlers, interfaces, DTOs
â”‚   â”œâ”€â”€ MVE.Infrastructure  # EF Core, Identity, file storage, email
â”‚   â””â”€â”€ MVE.Web             # ASP.NET Core MVC, Areas, SignalR
â””â”€â”€ tests/
    â””â”€â”€ MVE.Application.UnitTests  # xUnit + EF InMemory + Moq
```

**Dependency rule:** Web â†’ Infrastructure â†’ Application â†’ Domain. Each outer layer references only the layer directly inside it; Domain has zero external dependencies.

---

## 2. Architecture Overview

```
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                        Web Layer                        â”‚
â”‚   MVC Controllers Â· Razor Views Â· Areas Â· SignalR Hub   â”‚
â”‚          Dispatches MediatR commands & queries          â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
                           â”‚ IRequest<T>
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                    Application Layer                     â”‚
â”‚   CQRS Handlers Â· FluentValidation Â· Pipeline Behavioursâ”‚
â”‚      Interfaces (IApplicationDbContext, IIdentityâ€¦)     â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
           â”‚ Implements interfaces
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                  Infrastructure Layer                    â”‚
â”‚  EF Core (SQL Server) Â· Identity Â· File Storage Â· Email â”‚
â”‚        AuditLogger Â· SignalRRealtimeNotifier            â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”¬â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
           â”‚ Entities only
â”Œâ”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â–¼â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”
â”‚                     Domain Layer                         â”‚
â”‚      Entities Â· Enums Â· Domain Events Â· Base Classes    â”‚
â””â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”˜
```

---

## 3. Domain Layer

### 3.1 Base Classes

#### `BaseAuditableEntity`
Every entity in the system inherits from this class.

```csharp
public abstract class BaseAuditableEntity
{
    public Guid Id { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedByUserId { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedByUserId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedByUserId { get; set; }

    // Domain events â€” published after SaveChanges via DispatchDomainEventsInterceptor
    [NotMapped]
    public IReadOnlyCollection<BaseDomainEvent> DomainEvents { get; }
    public void AddDomainEvent(BaseDomainEvent ev);
    public void ClearDomainEvents();
}
```

**Key points:**
- `IsDeleted` â€” all entities use **soft delete**. EF Core global query filters automatically exclude deleted rows from every query.
- `CreatedByUserId` / `UpdatedByUserId` / `DeletedByUserId` â€” automatically stamped by `AuditableEntityInterceptor` on `SaveChanges`.
- Domain events are **not persisted** â€” they are dispatched after save and handled by MediatR `INotificationHandler` implementations.

#### `BaseDomainEvent`

```csharp
public abstract class BaseDomainEvent : INotification
{
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
```

Implements MediatR `INotification` so any `INotificationHandler<TEvent>` can subscribe to it.

---

### 3.2 Entities

#### `ApplicationUser` *(Infrastructure/Identity)*
Extends ASP.NET Core Identity `IdentityUser`.

| Property | Type | Notes |
|---|---|---|
| `FullName` | `string` | Required |
| `ProfileImageUrl` | `string?` | Avatar URL |
| `CreatedAtUtc` | `DateTime` | Set on construction |

---

#### `Vendor`
One `Vendor` per user who has been approved to sell.

| Property | Type | Notes |
|---|---|---|
| `OwnerUserId` | `string` | FK â†’ `ApplicationUser.Id` |
| `BusinessName` | `string` | max 200 |
| `TaxNumber` | `string?` | max 50 |
| `IsApproved` | `bool` | Admin-controlled gate |
| `DefaultCommissionPercent` | `decimal` | Default 10%, precision (5,2), snapshotted onto orders |
| `Stores` | `ICollection<VendorStore>` | Navigation |

---

#### `VendorStore`
A vendor can own multiple stores. Each store is the unit of fulfillment.

| Property | Type | Notes |
|---|---|---|
| `VendorId` | `Guid` | FK â†’ `Vendor` |
| `Name` | `string` | max 200 |
| `Slug` | `string?` | Unique index, max 220 |
| `Description` | `string?` | max 2000 |
| `IsActive` | `bool` | Default true |
| `LogoUrl` / `BannerUrl` | `string?` | max 500 each |
| `ContactEmail` / `ContactPhone` | `string?` | |
| `Products` | `ICollection<Product>` | |
| `Coupons` | `ICollection<Coupon>` | |

---

#### `Category`
Hierarchical (self-referencing) with `ParentCategoryId`.

| Property | Type | Notes |
|---|---|---|
| `Name` | `string` | max 120 |
| `Slug` | `string` | Unique index, max 140 |
| `Description` | `string?` | max 500 |
| `IconUrl` | `string?` | max 500 |
| `ParentCategoryId` | `Guid?` | Self-ref, `DeleteBehavior.Restrict` |
| `DisplayOrder` | `int` | For sorting |
| `IsActive` | `bool` | |

---

#### `Product`
The vendor's catalog item. Buying happens through `ProductVariant`.

| Property | Type | Notes |
|---|---|---|
| `VendorStoreId` | `Guid` | FK â†’ `VendorStore`, `Restrict` |
| `CategoryId` | `Guid` | FK â†’ `Category`, `Restrict` |
| `Name` | `string` | max 200 |
| `Slug` | `string` | Unique index, max 220 |
| `Description` | `string?` | max 4000 |
| `BasePrice` | `decimal` | Display price, precision (18,2) |
| `IsPublished` | `bool` | Vendor-controlled visibility |
| `IsFeatured` | `bool` | Admin-controlled homepage feature |
| `ApprovalStatus` | `ProductApprovalStatus` | Default `Pending`; only Admin can set `Approved` |
| `AverageRating` | `decimal` | Denormalized, precision (3,2) |
| `ReviewCount` | `int` | Denormalized |
| `ViewCount` | `int` | Denormalized |
| `Images` | `ICollection<ProductImage>` | |
| `Variants` | `ICollection<ProductVariant>` | Actual purchasable SKUs |
| `Reviews` | `ICollection<Review>` | |
| `WishlistItems` | `ICollection<WishlistItem>` | |

---

#### `ProductImage`

| Property | Type | Notes |
|---|---|---|
| `ProductId` | `Guid` | FK â†’ `Product`, `Cascade` |
| `Url` | `string` | Relative or absolute URL |
| `IsMain` | `bool` | Primary display image |
| `SortOrder` | `int` | |

---

#### `ProductVariant`
The actual unit that goes into the cart and order. Holds price and stock.

| Property | Type | Notes |
|---|---|---|
| `ProductId` | `Guid` | FK â†’ `Product`, `Cascade` |
| `Sku` | `string` | Unique index, max 80 |
| `Name` | `string?` | Display name (e.g. "Blue / XL"), max 120 |
| `Color` | `string?` | max 50 |
| `Size` | `string?` | max 20 |
| `Price` | `decimal` | Actual sale price, precision (18,2) |
| `StockQuantity` | `int` | Decremented atomically at checkout |
| `IsActive` | `bool` | Default true |

---

#### `Cart`
One cart per authenticated customer. Unique index on `CustomerUserId`.

| Property | Type | Notes |
|---|---|---|
| `CustomerUserId` | `string` | Unique index, max 450 |
| `Items` | `ICollection<CartItem>` | |

#### `CartItem`

| Property | Type | Notes |
|---|---|---|
| `CartId` | `Guid` | FK â†’ `Cart`, `Cascade` |
| `ProductVariantId` | `Guid` | FK â†’ `ProductVariant`, `Restrict` |
| `Quantity` | `int` | |
| `UnitPrice` | `decimal` | Price at time of adding to cart, precision (18,2) |

---

#### `Order`
The central aggregate. Created by `PlaceOrderCommand`. Shipping address fields are **snapshot-copied** at checkout time so later address edits do not corrupt order history.

| Property | Type | Notes |
|---|---|---|
| `CustomerUserId` | `string` | max 450 |
| `OrderNumber` | `string` | Unique index, format `ORD-{yyyyMMdd}-{6hex}` |
| `ShippingAddressId` | `Guid?` | FK, `DeleteBehavior.NoAction` (cycle break) |
| `BillingAddressId` | `Guid?` | FK, `DeleteBehavior.NoAction` |
| `ShippingFullName/Phone/Line1/Line2/City/State/PostalCode/Country` | `string?` | Address snapshot |
| `Status` | `OrderStatus` | State machine |
| `Subtotal/TaxAmount/ShippingAmount/DiscountAmount/Total` | `decimal` | precision (18,2) |
| `CouponId` | `Guid?` | FK â†’ `Coupon`, `SetNull` on delete |
| `PlacedAtUtc` | `DateTime?` | |
| `PaidAtUtc` | `DateTime?` | |
| `CancelledAtUtc` | `DateTime?` | |
| `RefundRequestedAtUtc` | `DateTime?` | Customer RMA request |
| `RefundReason` | `string?` | max 2000 |
| `RefundedAtUtc` | `DateTime?` | |
| `Items` | `ICollection<OrderItem>` | |
| `Payments` | `ICollection<Payment>` | |
| `Shipments` | `ICollection<Shipment>` | |

---

#### `OrderItem`
Immutable snapshot â€” fields are never mutated after creation.

| Property | Type | Notes |
|---|---|---|
| `OrderId` | `Guid` | FK â†’ `Order`, `Cascade` |
| `ProductVariantId` | `Guid` | FK â†’ `ProductVariant`, `Restrict` |
| `VendorStoreId` | `Guid` | Denormalized for vendor dashboard queries, FK `Restrict` |
| `ProductName` | `string` | Snapshot, max 200 |
| `VariantName` | `string?` | Snapshot, max 120 |
| `Quantity` | `int` | |
| `UnitPrice` | `decimal` | Snapshot, precision (18,2) |
| `LineTotal` | `decimal` | `UnitPrice Ã— Quantity`, precision (18,2) |
| `CommissionPercent` | `decimal` | Snapshot from `Vendor.DefaultCommissionPercent`, precision (5,2) |
| `CommissionAmount` | `decimal` | `LineTotal Ã— CommissionPercent / 100`, precision (18,2) |
| `VendorNetAmount` | `decimal` | `LineTotal âˆ’ CommissionAmount`, precision (18,2) |
| `VendorFulfillmentStatus` | `VendorOrderItemStatus` | Default `PendingFulfillment` |

---

#### `Shipment`
One shipment per vendor per order (multi-vendor orders produce multiple shipments).

| Property | Type | Notes |
|---|---|---|
| `OrderId` | `Guid` | FK â†’ `Order`, `Cascade` |
| `VendorStoreId` | `Guid` | FK â†’ `VendorStore`, `Restrict` |
| `AssignedDeliveryUserId` | `string?` | Optional delivery user, max 450 |
| `Carrier` | `string?` | max 80 |
| `TrackingNumber` | `string?` | max 120 |
| `Status` | `ShipmentStatus` | |
| `ShippedAtUtc` | `DateTime?` | |
| `EstimatedDeliveryAtUtc` | `DateTime?` | |
| `DeliveredAtUtc` | `DateTime?` | |

---

#### `Payment`

| Property | Type | Notes |
|---|---|---|
| `OrderId` | `Guid` | FK â†’ `Order`, `Cascade` |
| `Amount` | `decimal` | precision (18,2) |
| `Status` | `PaymentStatus` | |
| `Provider` | `string?` | e.g. "CreditCard", max 80 |
| `ExternalPaymentId` | `string?` | max 200 |

---

#### `Address`

| Property | Type | Notes |
|---|---|---|
| `UserId` | `string` | Indexed, max 450 |
| `Label` | `string?` | e.g. "Home", max 50 |
| `Line1` | `string` | max 200 |
| `Line2` | `string?` | max 200 |
| `City` | `string` | max 100 |
| `State` | `string?` | max 100 |
| `PostalCode` | `string` | max 20 |
| `Country` | `string` | max 100 |
| `Phone` | `string?` | max 50 |
| `IsDefault` | `bool` | |

---

#### `Coupon`

| Property | Type | Notes |
|---|---|---|
| `Code` | `string` | Unique index, max 40, stored uppercase |
| `DiscountType` | `CouponDiscountType` | Percentage or FixedAmount |
| `DiscountValue` | `decimal` | precision (18,2) |
| `MinimumOrderAmount` | `decimal` | Cart subtotal threshold, precision (18,2) |
| `MaxUses` | `int?` | Global usage cap |
| `MaxUsesPerCustomer` | `int?` | Per-customer cap |
| `UsedCount` | `int` | Incremented on each order |
| `StartsAtUtc` | `DateTime?` | |
| `ExpiresAtUtc` | `DateTime?` | |
| `IsActive` | `bool` | Default true |
| `VendorStoreId` | `Guid?` | **null = platform-wide (admin)**, non-null = vendor-scoped |

---

#### `Review`

| Property | Type | Notes |
|---|---|---|
| `ProductId` | `Guid` | FK â†’ `Product`, `Cascade` |
| `CustomerUserId` | `string` | max 450 |
| `OrderItemId` | `Guid?` | Optional verified-purchase link, `SetNull` on delete |
| `Rating` | `int` | 1â€“5 |
| `Title` | `string?` | max 200 |
| `Comment` | `string?` | max 2000 |
| `IsApproved` | `bool` | Admin moderation gate; hidden from public until `true` |
| `VendorReply` | `string?` | max 2000 |
| `VendorRepliedAtUtc` | `DateTime?` | |

---

#### `WishlistItem`
Composite unique index on `(CustomerUserId, ProductId)` prevents duplicates.

| Property | Type | Notes |
|---|---|---|
| `CustomerUserId` | `string` | max 450 |
| `ProductId` | `Guid` | FK â†’ `Product`, `Cascade` |

---

#### `Notification`

| Property | Type | Notes |
|---|---|---|
| `UserId` | `string` | Indexed, max 450 |
| `Title` | `string` | max 200 |
| `Body` | `string` | max 2000 |
| `Type` | `NotificationType` | |
| `ActionUrl` | `string?` | In-app navigation URL, max 500 |
| `IsRead` | `bool` | |
| `ReadAtUtc` | `DateTime?` | |

---

#### `AuditLog`
Does **not** have a global soft-delete filter â€” audit logs are never deleted.

| Property | Type | Notes |
|---|---|---|
| `UserId` | `string?` | Indexed |
| `Action` | `AuditAction` | |
| `EntityName` | `string` | max 120, indexed |
| `EntityId` | `string?` | max 80 |
| `OldValues` | `string?` | JSON snapshot |
| `NewValues` | `string?` | JSON snapshot |
| `IpAddress` | `string?` | max 50, captured from `HttpContext.Connection.RemoteIpAddress` |

---

#### `RefreshToken`

| Property | Type | Notes |
|---|---|---|
| `UserId` | `string` | Indexed, max 450 |
| `TokenHash` | `string` | Unique index, max 500 |
| `ReplacedByTokenHash` | `string?` | max 500 |

---

### 3.3 Enumerations

| Enum | Values |
|---|---|
| `OrderStatus` | `PendingPayment=0`, `Paid=1`, `Processing=2`, `Shipped=3`, `Delivered=4`, `Cancelled=5`, `Refunded=6` |
| `PaymentStatus` | `Pending=0`, `Authorized=1`, `Captured=2`, `Failed=3`, `Refunded=4` |
| `PaymentMethod` | Credit card / mock values |
| `ShipmentStatus` | `Pending=0`, `InTransit=1`, `Delivered=2`, `Failed=3` |
| `VendorOrderItemStatus` | `PendingFulfillment=0`, `Processing=1`, `ReadyToShip=2`, `Shipped=3`, `Delivered=4`, `Cancelled=5` |
| `ProductApprovalStatus` | `Pending=0`, `Approved=1`, `Rejected=2` |
| `CouponDiscountType` | `Percentage=0`, `FixedAmount=1` |
| `AuditAction` | `Create=0`, `Update=1`, `Delete=2`, `Login=3`, `Logout=4`, `PasswordReset=5`, `Other=6` |
| `NotificationType` | `OrderUpdate=0`, `Promotion=1`, `System=2`, `Account=3` |

---

### 3.4 Domain Events

#### `OrderPlacedEvent`

```csharp
public sealed class OrderPlacedEvent : BaseDomainEvent
{
    public Guid OrderId { get; }
    public string OrderNumber { get; }
    public string CustomerUserId { get; }
    public decimal Total { get; }
}
```

Raised inside `PlaceOrderCommand`. Published by `DispatchDomainEventsInterceptor` after `SaveChangesAsync`. Handled by `OrderPlacedAuditHandler` which writes an `AuditLog` entry.

---

## 4. Application Layer

### 4.1 Common Infrastructure

#### `PaginatedList<T>`

```csharp
public class PaginatedList<T>
{
    public IReadOnlyCollection<T> Items { get; }
    public int PageNumber { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

    // EF Core â€“ issues COUNT + Skip/Take in one round trip
    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source, int pageNumber, int pageSize, CancellationToken ct);
}
```

Handlers that aggregate in memory (e.g. `GetMyCustomersQuery`) build the list manually then use the constructor directly.

#### `Result` / `Result<T>`

```csharp
public class Result
{
    public bool Succeeded { get; init; }
    public string[] Errors { get; init; }
    public static Result Success();
    public static Result Failure(params string[] errors);
}

public class Result<T> : Result
{
    public T? Value { get; init; }
    public static Result<T> Success(T value);
    public new static Result<T> Failure(params string[] errors);
}
```

All command handlers return `Result` or `Result<T>`. Controllers check `result.Succeeded` and add `ModelState` errors or set `TempData`.

#### Exceptions

| Exception | Usage |
|---|---|
| `NotFoundException` | Entity not found |
| `ForbiddenAccessException` | User not authorized to the resource |
| `ValidationException` | FluentValidation failures (thrown by `ValidationBehaviour`) |

---

### 4.2 Interfaces

| Interface | Implemented By | Purpose |
|---|---|---|
| `IApplicationDbContext` | `ApplicationDbContext` | All DbSets + `SaveChangesAsync` |
| `IIdentityService` | `IdentityService` | Auth operations, user CRUD, role management |
| `ICurrentUserService` | `CurrentUserService` | Read `UserId`, `UserName`, `IsInRole` from HTTP context claims |
| `IFileStorageService` | `LocalFileStorageService` | Save/delete files to `wwwroot/uploads/` |
| `IEmailService` | `ConsoleEmailService` | Send transactional email (logs to console in dev) |
| `IAuditLogger` | `AuditLogger` | Write `AuditLog` entries with IP address |
| `IRealtimeNotifier` | `SignalRRealtimeNotifier` | Push live notifications via SignalR |
| `IDateTimeService` | `DateTimeService` | Provides `UtcNow` (mockable in tests) |

---

### 4.3 MediatR Pipeline Behaviours

Registered in order: `UnhandledExceptionBehaviour` â†’ `ValidationBehaviour`

#### `UnhandledExceptionBehaviour<TRequest, TResponse>`
Catches any unhandled exception, logs it with the full request payload, then re-throws.

#### `ValidationBehaviour<TRequest, TResponse>`
Resolves all `IValidator<TRequest>` implementations from DI. If any FluentValidation rule fails, throws `ValidationException` before the handler runs.

---

### 4.4 Feature Folders

All handlers follow the same structure: a `sealed record` for the request + `sealed class` handler + optional `AbstractValidator`.

#### Identity
| Handler | Input | Output |
|---|---|---|
| `RegisterCustomerCommand` | email, password, fullName | `Result<string>` (userId) |
| `RegisterVendorCommand` | email, password, fullName, businessName | `Result<string>` (userId) |

#### Categories
| Handler | Filters / Input | Output |
|---|---|---|
| `GetCategoriesListQuery` | â€” | `List<CategoryDto>` |
| `GetCategoryByIdQuery` | id | `CategoryDto` |
| `GetCategoryLookupQuery` | â€” | `List<CategoryLookupDto(Guid Id, string Name)>` |
| `CreateCategoryCommand` | name, slug, description, parentId, displayOrder | `Result<Guid>` |
| `UpdateCategoryCommand` | id + fields | `Result` |
| `DeleteCategoryCommand` | id | `Result` |

#### Vendors
| Handler | Filters / Input | Output |
|---|---|---|
| `GetVendorsListQuery` | search?, approved?, page, pageSize | `PaginatedList<VendorListItemDto>` |
| `GetVendorByIdQuery` | id | `VendorDetailDto` |
| `GetMyVendorQuery` | (current user) | `VendorDetailDto?` |
| `SetVendorApprovalCommand` | id, isApproved | `Result` |
| `UpdateVendorCommissionCommand` | id, percent | `Result` |

#### Users
| Handler | Filters / Input | Output |
|---|---|---|
| `GetUsersListQuery` | search?, role?, page, pageSize=15 | `PaginatedList<UserSummary>` |
| `GetUserByIdQuery` | id | `UserSummary?` |
| `UpdateUserRolesCommand` | userId, roles[] | `Result` |

#### VendorStores
| Handler | Input | Output |
|---|---|---|
| `GetMyStoreQuery` | (current user) | `VendorStoreDto?` |
| `UpdateMyStoreCommand` | name, description, logo, banner, etc. | `Result` |
| `GetPublicStoreBySlugQuery` | slug | `PublicStoreDto?` |
| `GetPublicStoresQuery` | â€” | `List<PublicStoreDto>` |

#### Products
| Handler | Filters / Input | Output |
|---|---|---|
| `GetMyProductsQuery` | search?, approvalStatus?, categoryId?, page, pageSize=15 | `PaginatedList<MyProductListItemDto>` |
| `GetMyProductByIdQuery` | id | `MyProductDetailDto?` |
| `GetProductsForAdminQuery` | status?, search?, categoryId?, vendorSearch?, page, pageSize=15 | `PaginatedList<AdminProductListItemDto>` |
| `GetProductForAdminQuery` | id | `AdminProductDetailDto?` |
| `GetPublicProductsQuery` | category?, search?, sort?, page | `PaginatedList<PublicProductListItemDto>` |
| `GetPublicProductBySlugQuery` | slug | `PublicProductDetailDto?` |
| `GetFeaturedProductsQuery` | â€” | `List<FeaturedProductDto>` |
| `CreateProductCommand` | name, slug, categoryId, basePrice, description, images, variants | `Result<Guid>` |
| `UpdateProductCommand` | id + fields | `Result` |
| `DeleteProductCommand` | id | `Result` |
| `SetProductApprovalCommand` | id, status | `Result` |

#### Cart
| Handler | Input | Output |
|---|---|---|
| `GetMyCartQuery` | (current user) | `CartDto` |
| `GetMyCartCountQuery` | (current user) | `int` |
| `AddToCartCommand` | variantId, quantity | `Result` |
| `UpdateCartItemCommand` | cartItemId, quantity | `Result` |
| `RemoveCartItemCommand` | cartItemId | `Result` |

#### Orders
| Handler | Input | Output |
|---|---|---|
| `GetCheckoutSummaryQuery` | (current user cart) | `CheckoutSummaryDto` |
| `PlaceOrderCommand` | shippingAddressId, billingAddressId?, paymentMethod, couponCode? | `Result<Guid>` (orderId) |
| `GetMyOrdersQuery` | page, pageSize | `PaginatedList<MyOrderListItemDto>` |
| `GetMyOrderByIdQuery` | id | `MyOrderDetailDto?` |
| `CancelOrderCommand` | id | `Result` |

#### VendorOrders
| Handler | Filters / Input | Output |
|---|---|---|
| `GetVendorOrdersQuery` | status?, search?, dateFrom?, dateTo?, page, pageSize=15 | `PaginatedList<VendorOrderListItemDto>` |
| `GetVendorOrderByIdQuery` | orderId | `VendorOrderDetailDto?` |
| `UpdateVendorItemStatusCommand` | orderItemId, newStatus | `Result` |
| `GetVendorSalesChartQuery` | days | `List<DailyPointDto>` |
| `GetMyCustomersQuery` | search?, page, pageSize=15 | `PaginatedList<MyCustomerDto>` *(in-memory aggregation over OrderItems)* |

#### Shipments
| Handler | Filters / Input | Output |
|---|---|---|
| `GetMyShipmentsQuery` | status?, search?, page, pageSize=15 | `PaginatedList<VendorShipmentListItemDto>` |
| `CreateShipmentCommand` | orderId, carrier, trackingNumber | `Result<Guid>` |
| `MarkShipmentDeliveredCommand` | shipmentId | `Result` |

#### Reviews
| Handler | Filters / Input | Output |
|---|---|---|
| `GetAdminReviewsQuery` | rating?, productSearch?, approved?, page, pageSize=15 | `PaginatedList<AdminReviewDto>` |
| `GetVendorReviewsQuery` | isApproved?, rating?, productSearch?, page, pageSize=15 | `PaginatedList<VendorReviewDto>` |
| `CreateReviewCommand` | productId, rating, title, comment, orderItemId? | `Result<Guid>` |
| `SetApprovalCommand` *(moderation)* | id, isApproved | `Result` |
| `VendorReplyCommand` *(moderation)* | id, reply | `Result` |

#### Coupons
| Handler | Filters / Input | Output |
|---|---|---|
| `GetPlatformCouponsQuery` | search?, isActive?, page, pageSize=15 | `PaginatedList<CouponDto>` |
| `GetMyCouponsQuery` | search?, isActive?, page, pageSize=15 | `PaginatedList<CouponDto>` |
| `ValidateCouponQuery` | code, cartSubtotal, userId | `CouponValidationResult` |
| `CreateCouponCommand` | code, type, value, minOrder, maxUses, window, vendorStoreId? | `Result<Guid>` |
| `UpdateCouponCommand` | id + fields | `Result` |
| `DeleteCouponCommand` | id | `Result` |

#### Refunds
| Handler | Filters / Input | Output |
|---|---|---|
| `GetRefundRequestsQuery` | pendingOnly, search?, dateFrom?, dateTo?, page, pageSize=15 | `PaginatedList<RefundRequestDto>` |
| `RequestRefundCommand` | orderId, reason | `Result` |
| `ApproveRefundCommand` | orderId | `Result` |
| `RejectRefundCommand` | orderId | `Result` |

#### AuditLogs
| Handler | Filters / Input | Output |
|---|---|---|
| `GetAuditLogsQuery` | action?, userSearch?, dateFrom?, dateTo?, page, pageSize=20 | `PaginatedList<AuditLogDto>` |

#### Admin Dashboard
| Handler | Input | Output |
|---|---|---|
| `GetAdminDashboardQuery` | days=14 | `AdminDashboardDto` (counts + daily revenue chart) |

#### Miscellaneous
| Feature | Handlers |
|---|---|
| Addresses | GetMyAddresses, GetMyAddressById, Create/Update/Delete |
| Wishlist | GetMyWishlist, ToggleWishlistItem |
| Notifications | GetMyNotifications, MarkNotificationRead, MarkAllRead |
| Profile | GetMyProfile, UpdateProfile, ChangePassword |
| ProductImages | AddImage, SetMain, DeleteImage |
| ProductVariants | AddVariant, UpdateVariant, DeleteVariant, GetLowStockVariants |

---

## 5. Infrastructure Layer

### 5.1 Identity

**`ApplicationUser : IdentityUser`**

Extends the standard Identity user with `FullName`, `ProfileImageUrl`, and `CreatedAtUtc`.

**Roles** (`Infrastructure.Identity.Roles`):

| Role | Description |
|---|---|
| `Admin` | Full platform access |
| `Vendor` | Vendor area access |
| `Customer` | Storefront, cart, checkout |
| `Delivery` | Delivery assignment feature (reserved) |

**`IdentityService`** wraps `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`:

- `GetUsersPagedAsync` â€” applies search filter (Email/FullName), then role filter using `GetUsersInRoleAsync` to build an ID HashSet, then EF Core `Skip/Take`, then per-user `GetRolesAsync` loop.
- `SetUserRolesAsync` â€” diffs current vs. desired role sets, calls `RemoveFromRolesAsync` + `AddToRolesAsync`.
- Password policy: requires digit, lower, upper; no special char required; min length 6.
- Cookie auth: 14-day sliding expiry, login path `/Account/Login`.

---

### 5.2 DbContext

**`ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext`**

Identity tables are renamed for a cleaner schema:

| EF Default | Renamed To |
|---|---|
| `AspNetUsers` | `Users` |
| `AspNetRoles` | `Roles` |
| `AspNetUserRoles` | `UserRoles` |
| `AspNetUserClaims` | `UserClaims` |
| `AspNetUserLogins` | `UserLogins` |
| `AspNetRoleClaims` | `RoleClaims` |
| `AspNetUserTokens` | `UserTokens` |

Fluent API configurations are loaded automatically via `ApplyConfigurationsFromAssembly`.

Two **SaveChanges interceptors** are registered as scoped services and wired into the `DbContextOptions`:
1. `AuditableEntityInterceptor`
2. `DispatchDomainEventsInterceptor`

---

### 5.3 EF Core Configurations

**Global query filters** â€” all entities except `AuditLog` have `HasQueryFilter(e => !e.IsDeleted)`.

| Entity | Key Constraints | Cascade / Delete Behaviors | Indexes |
|---|---|---|---|
| `Vendor` | â€” | â€” | `OwnerUserId` |
| `VendorStore` | â€” | FK to Vendor: `Restrict` | `Slug` (unique) |
| `Category` | â€” | Self-ref parent: `Restrict` | `Slug` (unique) |
| `Product` | â€” | VendorStore/Category: `Restrict` | `Slug` (unique), `VendorStoreId`, `CategoryId` |
| `ProductVariant` | â€” | Product: `Cascade` | `Sku` (unique), `ProductId` |
| `Cart` | â€” | â€” | `CustomerUserId` (unique) |
| `CartItem` | â€” | Cart: `Cascade`, Variant: `Restrict` | â€” |
| `Order` | `OrderNumber` unique | Address FKs: `NoAction` (cycle break), Coupon: `SetNull` | `OrderNumber` (unique), `CustomerUserId` |
| `OrderItem` | â€” | Order: `Cascade`, Variant/Store: `Restrict` | `OrderId`, `VendorStoreId` |
| `Shipment` | â€” | Order: `Cascade`, Store: `Restrict` | â€” |
| `Payment` | â€” | Order: `Cascade` | â€” |
| `Coupon` | `Code` unique | Store: `Cascade` | `Code` (unique) |
| `Review` | â€” | Product: `Cascade`, OrderItem: `SetNull` | `ProductId` |
| `WishlistItem` | â€” | Product: `Cascade` | `(CustomerUserId, ProductId)` (unique) |
| `Address` | â€” | â€” | `UserId` |
| `Notification` | â€” | â€” | `UserId` |
| `AuditLog` | *(no soft delete filter)* | â€” | `UserId`, `EntityName` |
| `RefreshToken` | â€” | â€” | `UserId`, `TokenHash` |

---

### 5.4 EF Core Interceptors

#### `AuditableEntityInterceptor` (SaveChangesInterceptor)
Runs on **both sync and async** `SavingChanges`:

- `Added` â†’ sets `Id = Guid.NewGuid()` if empty, `CreatedAtUtc`, `CreatedByUserId`
- `Modified` â†’ sets `UpdatedAtUtc`, `UpdatedByUserId`
- `Deleted` â†’ **converts to soft delete**: changes state back to `Modified`, sets `IsDeleted = true`, `DeletedAtUtc`, `DeletedByUserId`

#### `DispatchDomainEventsInterceptor` (SaveChangesInterceptor)
Runs **after** `SavedChangesAsync`:

1. Collects all entities with pending domain events from `ChangeTracker`
2. Clears events from each entity
3. Publishes each event via `IPublisher.Publish` (MediatR)

This guarantees domain events are only dispatched after the database transaction succeeds.

---

### 5.5 Service Implementations

#### `LocalFileStorageService`
Saves files to `{WebRootPath}/uploads/{folder}/{guid+extension}`. Returns a relative URL `/uploads/{folder}/{filename}`. Delete checks file existence before calling `File.Delete`.

#### `AuditLogger`
Writes an `AuditLog` entity directly via `ApplicationDbContext`. Captures the caller's IP from `HttpContext.Connection.RemoteIpAddress`. Calls `SaveChangesAsync` immediately (independent transaction).

#### `ConsoleEmailService`
Logs the email recipient, subject, and HTML body to `ILogger` â€” no actual SMTP in development.

#### `CurrentUserService`
Reads `ClaimTypes.NameIdentifier` and `ClaimTypes.Name` from `IHttpContextAccessor.HttpContext.User`.

#### `DateTimeService`
Returns `DateTime.UtcNow`. Registered as **singleton** â€” injectable in tests to mock time.

---

### 5.6 Database Seeding

`DbInitializer.InitializeAsync` runs on every application startup (idempotent):

1. **Applies pending EF Core migrations** (`Database.MigrateAsync()`)
2. **Creates roles**: Admin, Vendor, Customer, Delivery
3. **Admin user**: `admin@shop.com` / `Admin123!`
4. **5 root categories**: Electronics, Fashion, Home, Books, Sports
5. **2 demo vendors** (each with 5 approved products, 1 default variant per product):
   - `techgear@shop.com` / `Demo123!` â€” TechGear store (Electronics)
   - `stylehub@shop.com` / `Demo123!` â€” StyleHub store (Fashion)
6. **Demo customer**: `demo@shop.com` / `Demo123!`
7. **Platform coupon**: `WELCOME10` â€” 10% off, no minimum, no expiry

---

## 6. Web Layer

### 6.1 Startup & Middleware

**`Program.cs` pipeline:**

```
MVC + ViewLocalization (en/tr) + DataAnnotationsLocalization
AddLocalization
AddHttpContextAccessor
AddApplicationServices        â† MediatR + FluentValidation + AutoMapper
AddInfrastructureServices     â† EF Core + Identity + services
AddSignalR
AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
UseHttpsRedirection
UseStaticFiles
UseRequestLocalization        â† en / tr
UseRouting
UseAuthentication
UseAuthorization
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
MapControllerRoute "areas"    â†’ {area:exists}/{controller=Home}/{action=Index}/{id?}
MapControllerRoute "default"  â†’ {controller=Home}/{action=Index}/{id?}
MapHub<NotificationsHub>      â†’ /hubs/notifications
â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
SeedDatabaseAsync             â† on startup
```

**QuestPDF** is configured with `LicenseType.Community` for invoice PDF generation.

---

### 6.2 Admin Area

All controllers: `[Area("Admin")]` `[Authorize(Roles = "Admin")]`

| Controller | Actions | Key Filters |
|---|---|---|
| `HomeController` | `Index` (dashboard) | â€” |
| `UsersController` | `Index`, `Details`, `UpdateRoles` | search, role, page |
| `VendorsController` | `Index`, `Details`, `Approve`, `Reject` | approved?, search, page |
| `ProductsController` | `Index`, `Details`, `Approve`, `Reject` | status?, search, categoryId?, vendorSearch?, page |
| `CategoriesController` | `Index`, `Create`, `Edit`, `Delete` | â€” |
| `ReviewsController` | `Index`, `SetApproval` | approved?, rating?, productSearch?, page |
| `AuditLogsController` | `Index` | action?, userSearch?, dateFrom?, dateTo?, page |
| `RefundsController` | `Index`, `Approve`, `Reject` | pendingOnly, search?, dateFrom?, dateTo?, page |
| `CouponsController` | `Index`, `Create`, `Edit`, `Delete` | search?, isActive?, page |

---

### 6.3 Vendor Area

All controllers: `[Area("Vendor")]` `[Authorize(Roles = "Vendor")]`

| Controller | Actions | Key Filters |
|---|---|---|
| `HomeController` | `Index` (vendor dashboard) | â€” |
| `SettingsController` | `Index`, `Save` (store settings) | â€” |
| `ProductsController` | `Index`, `Create`, `Edit`, `Delete`, `Details` + variant/image management | search?, approvalStatus?, categoryId?, page |
| `OrdersController` | `Index`, `Details`, `UpdateItemStatus` | status?, search?, dateFrom?, dateTo?, page |
| `ShipmentsController` | `Index`, `Create`, `MarkDelivered` | status?, search?, page |
| `CustomersController` | `Index` | search?, page |
| `ReviewsController` | `Index`, `Reply` | approved?, rating?, productSearch?, page |
| `CouponsController` | `Index`, `Create`, `Edit`, `Delete` | search?, isActive?, page |

**Resource ownership enforcement:** Every vendor handler resolves `ICurrentUserService.UserId` and filters by the user's own `VendorStore`. The URL `id` is never trusted alone â€” the handler always verifies `VendorStore.Vendor.OwnerUserId == userId`.

---

### 6.4 Customer-Facing Controllers

| Controller | Purpose |
|---|---|
| `AccountController` | Login, Register, RegisterVendor, Logout, ChangePassword, AccessDenied |
| `ProfileController` | View and edit profile, upload avatar |
| `HomeController` | Landing page, featured products, contact |
| `ProductsController` | Public product listing, product detail page, search |
| `StoresController` | Public vendor store listing, store detail page |
| `CartController` | View cart, add/update/remove items, mini-cart count (AJAX) |
| `CheckoutController` | Checkout summary, `PlaceOrder` POST |
| `OrdersController` | My orders list, order detail, cancel order, request refund, download invoice (PDF) |
| `AddressesController` | CRUD for customer address book |
| `ReviewsController` | Submit review on a purchased product |
| `WishlistController` | Add/remove/list wishlist items |
| `NotificationsController` | List notifications, mark read, mark all read |
| `CultureController` | Set `culture` cookie for en/tr language switch |

---

### 6.5 Real-Time Notifications

**Architecture:**

```
Application handler
  â†“  IRealtimeNotifier.NotifyUserAsync(userId, title, body, actionUrl)
SignalRRealtimeNotifier
  â†“  IHubContext<NotificationsHub>.Clients.Group("user:{userId}").SendAsync("notify", payload)
NotificationsHub (browser connection)
  â†“  OnConnectedAsync â†’ Groups.AddToGroupAsync(connectionId, "user:{userId}")
JavaScript client
  â†‘  connection.on("notify", handler)
```

The hub requires authentication (`[Authorize]`). Each user can have multiple concurrent connections (multiple tabs); all receive the same push via the group.

---

### 6.6 Views & Shared Components

**Three layouts:**

| Layout | Used By |
|---|---|
| `_PublicLayout` | Customer-facing pages |
| `_AdminLayout` | Admin area + Vendor area |
| `_AuthLayout` | Login / Register pages |

**Shared partial: `_Pagination.cshtml`**

Tuple model: `(int PageNumber, int TotalPages, int TotalCount, bool HasPrevious, bool HasNext, Dictionary<string,string?> Route)`

- Always renders (even single-page results show prev/next and the page count)
- Smart ellipsis: always shows first/last page, Â±2 pages around current
- Uses `asp-all-route-data` to preserve all active filter parameters across page links
- Called from every list view: `@await Html.PartialAsync("_Pagination", (Model.PageNumber, Model.TotalPages, Model.TotalCount, Model.HasPrevious, Model.HasNext, route))`

**Filter state convention:** Controllers store filter values in `ViewBag` (e.g. `ViewBag.Search`, `ViewBag.Status`). Views read them back via `ViewBag.X as T?` to rebuild the filter form and the `route` dictionary for pagination.

**Localization:** Views use `IStringLocalizer` / `IViewLocalizer` with `.en.resx` / `.tr.resx` resource files. Language switched via `CultureController` which sets a `.AspNetCore.Culture` cookie.

**PDF Invoices:** `OrderInvoicePdf` (in `Web/Services/`) uses **QuestPDF** to generate a printable invoice from order data. Downloaded from the customer `OrdersController`.

---

## 7. Key Cross-Cutting Patterns

### CQRS via MediatR
Controllers never contain business logic. They call `_mediator.Send(command/query)` and handle the result:

```csharp
var result = await _mediator.Send(new PlaceOrderCommand(...));
if (!result.Succeeded)
{
    ModelState.AddModelError("", result.Errors.First());
    return View(model);
}
return RedirectToAction("Confirmation", new { id = result.Value });
```

### Two-Layer Validation
1. **DataAnnotations** on ViewModels â†’ jQuery Unobtrusive Validation (client-side)
2. **FluentValidation** on Commands â†’ `ValidationBehaviour` in the MediatR pipeline (server-side)

### Soft Delete
`AuditableEntityInterceptor` intercepts `Deleted` state and flips it to `Modified` with `IsDeleted = true`. Global query filters on all entities ensure deleted rows are invisible to all queries transparently.

### Commission Snapshot
At checkout, `PlaceOrderCommand` reads `Vendor.DefaultCommissionPercent` and writes `CommissionPercent`, `CommissionAmount`, and `VendorNetAmount` directly onto each `OrderItem`. These values are **never recalculated** â€” they represent the agreement at the moment of sale.

### Address Snapshot
`PlaceOrderCommand` copies all address fields from the `Address` entity onto the `Order`. This means editing or deleting an address later does not affect existing orders.

### Pagination Pattern
All list pages use `PaginatedList<T>`. The `route` dictionary in each view excludes null/empty filter values, so only active filters appear in pagination URLs.

---

## 8. Data Flow: Place an Order

```
Customer clicks "Place Order"
  â”‚
  â–¼
CheckoutController.PlaceOrder(CheckoutViewModel)
  â”‚  â†’ new PlaceOrderCommand(shippingAddressId, paymentMethod, couponCode?)
  â”‚
  â–¼
ValidationBehaviour
  â”‚  PlaceOrderCommandValidator: shippingAddressId NotEmpty
  â”‚
  â–¼
PlaceOrderCommandHandler.Handle(...)
  â”‚
  â”œâ”€ 1. Verify shipping address belongs to current user
  â”œâ”€ 2. Load cart with full Include chain (Variants â†’ Products â†’ VendorStores â†’ Vendors)
  â”œâ”€ 3. Re-validate stock & product availability for each cart item
  â”œâ”€ 4. Build Order entity with shipping address snapshot
  â”œâ”€ 5. For each CartItem:
  â”‚       â€¢ Create OrderItem with snapshot fields
  â”‚       â€¢ Calculate commission (LineTotal Ã— CommissionPercent / 100)
  â”‚       â€¢ Decrement ProductVariant.StockQuantity
  â”œâ”€ 6. Apply coupon (if provided):
  â”‚       â€¢ Check active, window, global uses, per-customer uses, minimum order
  â”‚       â€¢ Calculate discount (percentage or fixed)
  â”‚       â€¢ Increment Coupon.UsedCount
  â”œâ”€ 7. Set Order.Total = Subtotal âˆ’ Discount
  â”œâ”€ 8. Add Payment record (Status = Captured, Provider = PaymentMethod)
  â”œâ”€ 9. Clear cart (RemoveRange CartItems)
  â”œâ”€ 10. Add Order to DbContext
  â”œâ”€ 11. order.AddDomainEvent(new OrderPlacedEvent(...))
  â”œâ”€ 12. SaveChangesAsync
  â”‚       â””â”€ AuditableEntityInterceptor stamps Created/Updated fields
  â”‚       â””â”€ DispatchDomainEventsInterceptor publishes OrderPlacedEvent
  â”‚             â””â”€ OrderPlacedAuditHandler writes AuditLog entry
  â””â”€ 13. Send confirmation email (fire-and-forget via ConsoleEmailService)
  â”‚
  â–¼
Result<Guid>.Success(order.Id)
  â”‚
  â–¼
Controller redirects to Order Confirmation page
```

---

## 9. Default Credentials & Seed Data

| Role | Email | Password |
|---|---|---|
| Admin | `admin@shop.com` | `Admin123!` |
| Vendor | `techgear@shop.com` | `Demo123!` |
| Vendor | `stylehub@shop.com` | `Demo123!` |
| Customer | `demo@shop.com` | `Demo123!` |

**Sample coupon:** `WELCOME10` â€” 10% platform-wide discount, no minimum order, no expiry.

**Seed products:** 5 Electronics products (TechGear store) + 5 Fashion products (StyleHub store). All seeded with `ApprovalStatus = Approved`, `IsPublished = true`, one default variant each (`StockQuantity = 25`).

---

## 10. UI Template â€” AdminLTE 4

### What is AdminLTE?

[AdminLTE](https://adminlte.io/) is a free, open-source admin dashboard template built on top of Bootstrap 5. Version 4 (used here) is a ground-up rewrite that drops jQuery as a hard dependency, adopts Bootstrap 5's utility-first CSS, and ships a modern component set.

### How It Is Used in This Project

AdminLTE 4 drives the **entire back-office UI** â€” both the Admin area and the Vendor area share the same `_AdminLayout.cshtml`. The public storefront uses a separately designed `_PublicLayout.cshtml` based on plain Bootstrap 5.

| Layout | Used By | Template Base |
|---|---|---|
| `_AdminLayout.cshtml` | Admin area + Vendor area | AdminLTE 4 |
| `_PublicLayout.cshtml` | Customer-facing storefront | Bootstrap 5 |
| `_AuthLayout.cshtml` | Login / Register pages | Bootstrap 5 (centered card) |

### AdminLTE Components Used

| Component | Where |
|---|---|
| **Sidebar navigation** (`app-sidebar`) | Admin and Vendor left-side nav with collapsible sections, active link highlighting |
| **Top navbar** (`app-header`) | Notification bell (SignalR live count), user avatar dropdown, language switcher |
| **Cards** (`card`, `card-header`, `card-body`) | All dashboard stat widgets, form containers, detail panels |
| **Small boxes / Info boxes** | Admin dashboard KPI tiles (vendor count, order count, revenue) and Vendor dashboard tiles (products, pending orders, low stock) |
| **Data tables** (`table table-hover`) | All list pages â€” styled with AdminLTE's `mv-list-table` wrapper class |
| **Breadcrumbs** | Page titles with hierarchical back-links |
| **Alert / Toast** | `TempData["Success"]` and `TempData["Error"]` rendered as dismissible Bootstrap alerts |
| **Chart.js integration** | Admin revenue chart (14-day daily orders + revenue bars) and Vendor sales chart |
| **Custom pill badges** (`mv-pill`) | Status indicators throughout â€” `mv-pill-success`, `mv-pill-pending`, `mv-pill-danger`, `mv-pill-warning`, `mv-pill-info`, `mv-pill-muted`, `mv-pill-approved`, `mv-pill-rejected` |

### Custom CSS Conventions

The project defines its own utility classes on top of AdminLTE:

| Class | Purpose |
|---|---|
| `.mv-list` | Wrapper for every list/index page |
| `.mv-list-toolbar` | Flexbox toolbar row: title on left, action button on right |
| `.mv-list-title` | Page heading inside the toolbar |
| `.mv-list-table` | Table with consistent row hover and border styling |
| `.mv-empty` | Empty-state container (icon + heading + message + optional CTA) |
| `.mv-empty-icon` | Large icon inside empty state |
| `.mv-pill` | Base for all status badge pills |
| `.mv-pill-*` | Color variants: success (green), pending (yellow), danger (red), warning (orange), info (blue), muted (grey), approved (teal), rejected (red) |
| `--mv-grad-1` | CSS variable used for the customer avatar circle gradient |

### Bootstrap Icons

All icons use the **Bootstrap Icons** library (`bi bi-*`). Every status badge, button, nav item, and empty-state illustration uses an SVG icon from this set â€” no FontAwesome or other icon library is loaded.

---

## 11. Feature Catalogue by Role

This section lists every user-facing feature grouped by role, with a brief use-case description for each.

---

### 11.1 Customer Features

Customers are unauthenticated visitors who register and shop. Most features require authentication; browsing and searching are public.

---

#### 1. Home Page & Discovery
**Use case:** A visitor lands on the homepage and sees up to 8 featured products and a grid of active categories. They can immediately start browsing without signing in.
- Displays featured products (admin-curated via `IsFeatured = true`)
- Displays category grid with icons linking to filtered product lists
- Quick links to About, FAQ, Terms, Privacy pages

---

#### 2. Product Browsing & Search
**Use case:** A customer wants to find a specific product or explore a category. They apply filters and page through results.
- Publicly accessible (no login required)
- Filters: keyword search, category, store slug, minimum price, maximum price
- Paginated grid (12 per page)
- Each card shows product image, name, price, store name, and average rating

---

#### 3. Product Detail Page
**Use case:** A customer views a specific product, reads its description and reviews, selects a variant, and adds it to their cart.
- Accessible via SEO-friendly slug URL: `/Products/Details/{slug}`
- Shows all product images, description, all active variants (color, size, price, stock)
- Displays approved customer reviews with star ratings
- "Add to cart" button for each variant
- "Add to wishlist" button
- Inline review submission form (requires login; goes to moderation queue)

---

#### 4. Store Pages
**Use case:** A customer wants to see all products from a specific vendor store and learn about the store.
- `/Stores` â€” public listing of all active stores (logo, name, description)
- `/Stores/{slug}` â€” individual store page: banner, logo, contact info, paginated product grid

---

#### 5. Shopping Cart
**Use case:** A customer adds multiple items from different vendors, adjusts quantities, and prepares to check out. All cart interactions require authentication.
- Add item to cart (validates stock and product availability)
- View cart: line items with images, variant name, unit price, quantity, line total
- Update quantity inline (set to 0 removes the item)
- Remove individual items
- Mini-cart item count shown in navbar (AJAX-refreshed)
- Cart is persistent (stored in the database, not a cookie)

---

#### 6. Checkout & Order Placement
**Use case:** A customer reviews their cart, selects a shipping address and payment method, optionally applies a coupon code, and places the order.
- Checkout summary page shows itemised list, subtotal, discount, total
- Select from saved addresses or add new address inline
- Choose payment method (mock â€” always succeeds)
- Enter coupon code: validates active, within date window, usage limits, minimum order amount, per-customer limit
- `PlaceOrderCommand` atomically: checks stock, snapshots prices and commission, creates Order + OrderItems + Payment, clears cart, raises `OrderPlacedEvent`
- Confirmation email sent after successful placement

---

#### 7. Order History & Tracking
**Use case:** A customer wants to check the status of past orders or track their shipments.
- Paginated list of own orders (10 per page), sorted by date
- Order detail: line items, shipping address snapshot, payment status, shipment tracking numbers and status
- Cancel order (only while in eligible status)
- Request refund (after delivery, with reason text)
- Download invoice as PDF (generated by QuestPDF with order number, line items, totals)

---

#### 8. Address Book
**Use case:** A customer manages their saved delivery addresses to speed up future checkouts.
- List all personal addresses
- Add new address (label, line1/2, city, state, postal code, country, phone)
- Edit existing address
- Delete address
- Mark an address as default (pre-selected at checkout)

---

#### 9. Wishlist
**Use case:** A customer saves products they are interested in but not ready to buy yet.
- Add product to wishlist (from listing or detail page)
- View all wishlisted products with links back to product pages
- Remove individual items
- Composite unique constraint prevents duplicate entries per customer per product

---

#### 10. Product Reviews
**Use case:** After receiving an order, a customer writes a review for the product they purchased.
- Submit a star rating (1â€“5), optional title and optional comment
- Optionally linked to a specific `OrderItem` for a verified-purchase badge
- Review is hidden from the public product page until an admin approves it
- Confirmation message shown immediately: "Your review will appear once approved"

---

#### 11. User Profile
**Use case:** A customer updates their display name and avatar.
- View current profile: name, email, roles, avatar
- Edit full name
- Upload profile picture (saved to `wwwroot/uploads/users/`)
- Change password (requires current password)

---

#### 12. Notifications
**Use case:** A customer receives real-time alerts about order status changes or promotions and can manage their notification inbox.
- Notification bell in the navbar shows live unread count (SignalR push)
- Notification list page: title, body, type, timestamp, read/unread state
- Mark individual notification as read
- Mark all notifications as read

---

#### 13. Language Switching
**Use case:** A customer prefers to use the application in Turkish.
- Language toggle in the navbar sets an `.AspNetCore.Culture` cookie
- Supported languages: English (`en`) and Turkish (`tr`)
- All labels, validation messages, and page titles are localised via `.resx` resource files

---

#### 14. Contact Form
**Use case:** A visitor sends a message to the platform support team.
- Name, email, subject, message fields
- On submit, sends an email to `ahmadbkye12@gmail.com` via `IEmailService`
- Success confirmation shown after submission

---

### 11.2 Vendor Features

Vendors are approved business accounts that list and sell products. They access the system through the **Vendor Area** (`/Vendor/...`), which uses the AdminLTE layout. A vendor must be approved by an admin before they can sell.

---

#### 1. Vendor Dashboard
**Use case:** A vendor logs in and immediately sees the health of their business at a glance.
- KPI tiles: total stores, total products, pending order items (fulfillment status < Shipped), low-stock variants (â‰¤ 5 units)
- 14-day sales chart (Chart.js bar chart: daily order count + revenue)
- Navigation sidebar links to all vendor features

---

#### 2. Store Settings
**Use case:** A vendor customises their store's public-facing profile.
- Edit store name, description, contact email, contact phone
- Upload logo image (saved to `wwwroot/uploads/stores/`)
- Upload banner image
- Changes immediately reflected on the public store page

---

#### 3. Product Catalog Management
**Use case:** A vendor adds new products, edits existing ones, manages variants, and uploads images.

**Product list:**
- Paginated product list (15 per page)
- Filters: keyword search, category, approval status (Pending / Approved / Rejected)
- Columns: thumbnail, name/slug, category, store, base price, variant count, stock (highlighted red â‰¤ 0, orange â‰¤ 5), approval status badge, published badge

**Create product:**
- Name, slug (auto-generated from name), category, base price, description, publish toggle
- Upload multiple product images, set which is the main/primary image
- Add one or more variants inline (SKU, color, size, price, stock quantity)
- Product is submitted in `Pending` approval state â€” admin must approve before it appears publicly

**Edit product:**
- Update all product fields
- Add / remove / reorder images
- Add / edit / delete individual variants
- Toggle `IsPublished` independently of approval

**Delete product:** Soft-deleted â€” invisible to customers and removed from search results.

**Low stock alert:** A separate view lists all variants with stock â‰¤ 5 for the vendor's stores.

---

#### 4. Order Fulfillment
**Use case:** When a customer places an order containing the vendor's products, the vendor sees it and advances each item through the fulfillment pipeline.
- Paginated order list (15 per page)
- Filters: order status, keyword (order number or customer name), date range
- Columns: order number, customer name, placed date, item count, revenue, vendor net, pending item count, order status
- **Order detail:** full order line items (only the vendor's own items), shipping address snapshot, customer contact info
- **Update item status:** advance individual order items through `PendingFulfillment â†’ Processing â†’ ReadyToShip â†’ Shipped â†’ Delivered / Cancelled`
- **Create shipment:** enter carrier name, tracking number, estimated delivery date â€” this creates a `Shipment` entity linked to the order and vendor store

---

#### 5. Shipment Tracking
**Use case:** A vendor monitors all shipments they have created and marks them delivered when confirmed.
- Paginated shipments list (15 per page)
- Filters: shipment status (Pending / InTransit / Delivered / Failed), keyword (order number or tracking number)
- Columns: order number (linked to order detail), carrier, tracking number, status badge, shipped date, delivered date
- **Mark delivered:** changes shipment status from `InTransit` to `Delivered`

---

#### 6. Customer Insights
**Use case:** A vendor wants to understand who their repeat buyers are and how much each has spent with them.
- Derived from order history â€” not a direct customer database (the platform owns customer accounts)
- Paginated list (15 per page) with keyword search on customer name
- Columns: customer avatar (first-letter circle), full name, order count, total amount spent with this vendor, date of last order
- Data aggregated in-memory from `OrderItems` grouped by `ShippingFullName`

---

#### 7. Review Management
**Use case:** A vendor reads customer reviews for their products and replies to them publicly.
- Paginated card-based review list (15 per page)
- Filters: product name search, star rating (1â€“5), approval status (Pending / Approved)
- Each card shows: product name, star rating, approval badge, review title, comment, submission date
- **Reply to review:** text input to write a public vendor response (stored as `VendorReply` + `VendorRepliedAtUtc`)
- Replies are shown beneath the review on the public product detail page

---

#### 8. Coupon Management
**Use case:** A vendor creates discount codes scoped exclusively to their store to run promotions.
- Paginated coupon list (15 per page)
- Filters: code search, active/inactive status
- **Create coupon:** code (stored uppercase), discount type (Percentage / Fixed Amount), discount value, minimum order amount, max global uses, max uses per customer, start date, expiry date, active toggle
- Coupons created here have `VendorStoreId` set â€” they only apply to orders containing the vendor's products
- **Edit / Delete coupon** (soft delete)

---

### 11.3 Admin Features

Admins have full platform visibility and control. They access the system through the **Admin Area** (`/Admin/...`), which also uses the AdminLTE layout.

---

#### 1. Admin Dashboard
**Use case:** An admin opens the dashboard to get a real-time snapshot of the platform's health.
- KPI tiles: total vendors, pending vendor applications, total products, pending product approvals, total orders, distinct customer count, total platform revenue
- 14-day daily revenue and order count bar chart (Chart.js)
- Numbers exclude cancelled orders from revenue totals

---

#### 2. Vendor Management
**Use case:** When a new vendor registers, an admin reviews their application and approves or rejects it. Admins can also adjust a vendor's commission rate.
- Paginated vendor list (15 per page)
- Filters: approval status (approved / pending), keyword search on business name
- **Vendor detail:** business name, tax number, approval status, commission percent, list of stores
- **Approve vendor:** sets `IsApproved = true`, unlocking the vendor's ability to sell
- **Reject / revoke approval:** sets `IsApproved = false`, blocking the vendor from selling
- **Set commission %:** update `DefaultCommissionPercent` â€” applied to all *future* orders (existing order items are unaffected because they snapshot the rate)

---

#### 3. Product Approval
**Use case:** When a vendor submits a new or edited product, it enters a `Pending` state. An admin reviews it and either approves (making it visible to customers) or rejects it.
- Paginated product list (15 per page)
- Filters: approval status (Pending / Approved / Rejected), product name search, vendor/store search, category
- **Product detail:** full product information, images, variants, vendor info
- **Approve product:** sets `ApprovalStatus = Approved` â€” product becomes visible on the storefront
- **Reject product:** sets `ApprovalStatus = Rejected` â€” vendor is informed via status badge
- Products are always displayed with their current approval status in the vendor's own product list

---

#### 4. Category Management
**Use case:** An admin maintains the category hierarchy that vendors assign products to and customers browse by.
- Flat list of all categories
- **Create category:** name, slug, description, optional parent category (hierarchical), icon URL, display order, active toggle
- **Edit category:** all fields
- **Delete category:** soft-deleted â€” products referencing it are not affected (FK `Restrict` prevents deletion of categories with products)

---

#### 5. User Management
**Use case:** An admin searches for a specific user, views their profile and roles, and can reassign roles if needed.
- Paginated user list (15 per page)
- Filters: keyword search (email or full name), role filter (Admin / Vendor / Customer / Delivery)
- Columns: email, full name, joined date, assigned roles
- **User detail:** all profile information, current roles checklist
- **Update roles:** assign or remove any combination of roles (Admin, Vendor, Customer, Delivery) â€” implemented as a diff operation (remove old, add new)

---

#### 6. Review Moderation
**Use case:** Customer reviews are hidden by default. An admin approves reviews they consider legitimate or keeps them suppressed.
- Paginated review list (15 per page)
- Filters: product name search, star rating (1â€“5), approval status (Pending / Approved)
- Columns: product name, star rating, review title, truncated comment, submission date, approval status
- **Approve review:** sets `IsApproved = true` â€” review becomes visible on the product detail page and counts toward the product's `AverageRating` and `ReviewCount`
- **Unapprove review:** sets `IsApproved = false` â€” hides it from the public again

---

#### 7. Refund Management
**Use case:** After a customer requests a refund on a delivered order, an admin reviews the reason and decides to approve or decline.
- Paginated refund request list (15 per page)
- Filters: pending-only toggle (default: pending), keyword search (order number or customer name), date range
- Columns: order number, customer name, order total, refund reason, request date, current status
- **Approve refund:** sets `Order.Status = Refunded`, marks the linked `Payment.Status = Refunded`, records `RefundedAtUtc` â€” customer is informed via notification
- **Reject refund:** keeps the order status unchanged; an internal note can be recorded

---

#### 8. Coupon Management (Platform-Wide)
**Use case:** An admin creates promotional codes that apply across all stores, regardless of which vendors' products are in the order.
- Paginated coupon list (15 per page)
- Filters: code search, active/inactive status
- **Create coupon:** same fields as vendor coupons, but `VendorStoreId = null` making it platform-wide
- **Edit / Delete coupon** (soft delete)
- Platform coupons appear in the storefront alongside any vendor-scoped coupons

---

#### 9. Audit Log
**Use case:** A security officer or admin investigates a suspicious action by looking up who did what and when.
- Paginated audit log (20 per page), most recent first
- Filters: user ID search, action type (Create / Update / Delete / Login / Logout / PasswordReset / Other), date range (inclusive â€” `dateTo` extends to end of day)
- Columns: timestamp, user ID, action badge (colour-coded), entity name, entity ID (code), IP address
- Log entries are written by `AuditLogger` (direct `SaveChangesAsync` call, independent of other tracked changes)
- `OrderPlacedAuditHandler` demonstrates domain-event-driven audit logging: `OrderPlacedEvent` â†’ `AuditLog(Create, "Order", orderId)`
- Audit entries are **never soft-deleted** â€” they have no global query filter
