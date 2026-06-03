# MVE - Multi-Vendor E-Commerce Marketplace

MVE is a **multi-vendor e-commerce marketplace** built on **ASP.NET Core MVC 8** using **Clean Architecture**. It models a real marketplace where many independent vendors sell through a single storefront and shared checkout:

- **Customers** browse products across all stores, build one cart, check out once, and track their orders.
- **Vendors** run their own storefront - managing products, variants, stock, orders, shipments, coupons, and reviews - and earn the order value minus a platform commission.
- **Admins** govern the platform - approving vendors and products, moderating reviews, handling refunds, running platform-wide coupons, and auditing activity.

A single order can contain items from multiple vendors. The system splits each order into per-vendor line items and per-vendor shipments, so every vendor fulfills their own part independently while the customer sees one order.

> For the deep-dive (entity-by-entity model, EF configuration, request diagrams, and design rationale) see **[PROJECT_ARCHITECTURE.md](PROJECT_ARCHITECTURE.md)**.

---

## Table of contents

1. [Quick start](#quick-start)
2. [Default credentials](#default-credentials-auto-seeded)
3. [Architecture](#architecture)
4. [Domain model](#domain-model)
5. [How an order works](#how-an-order-works)
6. [Use cases](#use-cases)
   - [Customer use cases](#customer-use-cases)
   - [Vendor use cases](#vendor-use-cases)
   - [Admin use cases](#admin-use-cases)
7. [Tech stack](#tech-stack)
8. [Project layout](#project-layout)
9. [Configuration](#configuration)
10. [Running the tests](#running-the-tests)

---

## Quick start

### Prerequisites
- .NET 8 SDK
- SQL Server LocalDB (ships with Visual Studio, or installable standalone)

### Run
```bash
git clone <repo-url> MVE
cd MVE

# Run (migrations + seed run automatically on startup)
dotnet run --project src/MVE.Web --launch-profile http
```

Open: <http://localhost:5244>

The app applies EF Core migrations and seeds demo data on every startup (idempotent), so you do not need a manual database step. If you prefer to apply migrations by hand:

```bash
dotnet ef database update \
  --project src/MVE.Infrastructure \
  --startup-project src/MVE.Web
```

### Default credentials (auto-seeded)

| Role | Email | Password |
|---|---|---|
| **Admin** | `admin@shop.com` | `Admin123!` |
| **Vendor** (TechGear) | `techgear@shop.com` | `Demo123!` |
| **Vendor** (StyleHub) | `stylehub@shop.com` | `Demo123!` |
| **Customer** | `demo@shop.com` | `Demo123!` |

**Sample coupon:** `WELCOME10` - 10% off any order, platform-wide.

The seed also creates 5 categories and 10 approved products (5 per demo store), each with one default variant of 25 units.

---

## Architecture

The solution is split into four projects, one per layer. The arrow shows the **only** direction in which a layer is allowed to reference another - each layer depends on the one **inside** it and never the reverse:

```
+-----------------------------------------------------------+
|  MVE.Web            ASP.NET Core MVC                       |  outermost
|  Controllers, Razor Views, Areas, SignalR, ViewModels     |
+--------------------------+--------------------------------+
                           | references
+--------------------------v--------------------------------+
|  MVE.Infrastructure   EF Core, Identity, file storage,    |
|  email, SignalR notifier, audit, DbInitializer            |
+--------------------------+--------------------------------+
                           | references
+--------------------------v--------------------------------+
|  MVE.Application    CQRS handlers, validators, interfaces |
+--------------------------+--------------------------------+
                           | references
+--------------------------v--------------------------------+
|  MVE.Domain         entities, enums, events (pure C#)     |  innermost
+-----------------------------------------------------------+
```

### The dependency rule

Source-code dependencies point **inward only**. The Domain is at the center and references nothing; each outer layer may reference the layers inside it but never the ones outside it. The practical payoff:

- **The business rules do not depend on the framework.** The Domain and Application layers have no idea that EF Core, ASP.NET Core, or SignalR exist. You could swap SQL Server for another database, or the MVC front end for an API, without touching them.
- **Dependencies are inverted at the boundaries.** When an inner layer needs something from the outside world (a database, email, the current user), it declares a C# **interface** it owns, and an outer layer provides the implementation. So control flows outward at runtime while source dependencies still point inward. This is the Dependency Inversion Principle in action.
- **It is enforced by the project references**, not by convention - `MVE.Domain.csproj` has zero project references, so a stray `using Microsoft.EntityFrameworkCore;` in an entity simply will not compile.

### The four layers

#### 1. Domain (`MVE.Domain`) - the core

Pure C# with **no framework dependencies at all**. This is the heart of the system - the concepts that would still be true if the app were a console program or a mobile app.

- **Entities** (`Product`, `Order`, `OrderItem`, `Vendor`, `Coupon`, ...) - the business objects and the rules that are intrinsic to them.
- **Enums** - the state machines: `OrderStatus`, `PaymentStatus`, `ShipmentStatus`, `VendorOrderItemStatus`, `ProductApprovalStatus`, etc.
- **`BaseAuditableEntity`** - the base class every entity inherits, providing `Guid Id`, audit stamps (`CreatedBy`/`UpdatedBy`/`DeletedBy` + timestamps), the `IsDeleted` soft-delete flag, and a list of pending domain events.
- **Domain events** (`OrderPlacedEvent`) - facts that have happened, raised by entities and handled elsewhere after the transaction commits.

#### 2. Application (`MVE.Application`) - the use cases

References **Domain only**. This layer orchestrates the business operations - it is the catalog of everything the system can *do*, expressed as CQRS messages.

- **One folder per feature** (`Orders/`, `Products/`, `Cart/`, `Vendors/`, `Reviews/`, ...). Each use case is a MediatR `IRequest` (a command or query) plus an `IRequestHandler` that returns a `Result<T>`.
- **Interfaces it owns but does not implement** - `IApplicationDbContext`, `ICurrentUserService`, `IFileStorageService`, `IEmailService`, `IStripePaymentService`, `IDateTimeService`, `IRealtimeNotifier`. These are the seams through which it reaches the outside world without depending on it.
- **Pipeline behaviours** (`ValidationBehaviour`, `UnhandledExceptionBehaviour`) and **FluentValidation validators** that run around every handler.
- It knows about entities and abstractions - never about EF Core, HTTP, or any concrete technology.

#### 3. Infrastructure (`MVE.Infrastructure`) - the implementations

References **Application** (and transitively Domain). This is where the abstractions defined inside become real, talking to actual technology.

- **`ApplicationDbContext`** - the EF Core context (also the ASP.NET Identity store) that implements `IApplicationDbContext`, with Fluent API configurations and global soft-delete query filters.
- **SaveChanges interceptors** - `AuditableEntityInterceptor` (audit stamping + soft-delete conversion) and `DispatchDomainEventsInterceptor` (publishes domain events post-commit).
- **Concrete services** - `CurrentUserService`, `LocalFileStorageService`, `ConsoleEmailService`, `StripePaymentService`, `AuditLogger`, `DateTimeService`.
- **`DbInitializer`** - applies migrations and seeds roles, demo users, categories, and products on startup.

#### 4. Web (`MVE.Web`) - the entry point

References **Application + Infrastructure**. The thin outer shell that adapts HTTP to use cases and renders HTML. It contains as little logic as possible.

- **Controllers** map a `ViewModel` to a MediatR command/query, `_mediator.Send(...)` it, and translate the `Result<T>` into a view, a redirect, or `ModelState`/`TempData` feedback. **No business logic lives here.**
- **Razor Views + ViewModels**, with Admin and Vendor functionality isolated in `Areas/Admin` and `Areas/Vendor`.
- **Composition root** - `Program.cs` and the `AddApplicationServices` / `AddInfrastructureServices` extension methods wire the interfaces to their implementations. This is the one place that knows about every layer at once.
- **SignalR hub** for real-time notifications.

### Cross-cutting patterns

- **CQRS via MediatR** - controllers never contain business logic; they `_mediator.Send(...)` and act on the `Result<T>`.
- **Result pattern** - handlers return `Result<T>.Success(value)` or `Result<T>.Failure("message")`; controllers translate failures into `ModelState` errors or `TempData` toasts.
- **Two-layer validation** - DataAnnotations on ViewModels drive client-side jQuery validation; FluentValidation on commands runs server-side via `ValidationBehaviour` in the MediatR pipeline (alongside `UnhandledExceptionBehaviour`).
- **EF Core interceptors** - `AuditableEntityInterceptor` stamps CreatedBy/UpdatedBy and converts deletes into soft-deletes; `DispatchDomainEventsInterceptor` publishes domain events only after the transaction commits.
- **Soft delete everywhere** - a global EF query filter (`!IsDeleted`) hides deleted rows from every query automatically. (Audit logs are the one exception - they are never deleted.)
- **Resource ownership in handlers** - vendor-area handlers resolve `ICurrentUserService.UserId` and filter by the vendor's own stores. IDs from the URL are never trusted for authorization.
- **Real-time** - SignalR pushes live notifications (unread bell count, refund decisions, etc.) to a per-user group.

### Three layouts

`_PublicLayout` (storefront, Bootstrap 5), `_AdminLayout` (Admin + Vendor areas, AdminLTE 4), and `_AuthLayout` (login/register). Localized in English and Turkish via `IStringLocalizer` + `.resx`.

---

## Domain model

| Entity | Purpose |
|---|---|
| `ApplicationUser` | Identity user extended with `FullName`, `ProfileImageUrl`. |
| `Vendor` / `VendorStore` | An approved seller and their store(s). The store is the unit of fulfillment. |
| `Category` | Hierarchical (self-referencing) product category. |
| `Product` / `ProductImage` / `ProductVariant` | Catalog item, its images, and its purchasable SKUs. **Variants** hold price and stock. |
| `Cart` / `CartItem` | One persistent cart per customer; items reference a variant. |
| `Order` / `OrderItem` | The order aggregate. `OrderItem` is an immutable snapshot (name, price, commission) per vendor line. |
| `Shipment` | One per vendor per order; carries carrier, tracking, status. |
| `Payment` | Payment record for an order (mock or Stripe). |
| `Coupon` | Discount code. `VendorStoreId == null` = platform-wide; otherwise vendor-scoped. |
| `Review` | Customer rating + comment, hidden until admin-approved, with optional vendor reply. |
| `Address` | Customer address book entry. |
| `WishlistItem` / `Notification` | Saved products and in-app notifications. |
| `AuditLog` | Immutable record of who did what, when, from which IP. |
| `WebsiteSettings` / `StripeSettings` | Singleton platform configuration rows (branding/SEO and payment keys). |

**Key design decisions worth knowing:**

- **Commission snapshot** - at checkout each `OrderItem` copies the vendor's `DefaultCommissionPercent` and computes `CommissionAmount` and `VendorNetAmount`. These are never recalculated, so changing a vendor's rate later does not rewrite history.
- **Address snapshot** - the order copies the shipping address fields onto itself, so editing or deleting an address never corrupts past orders.
- **Multi-vendor split** - one cart produces one `Order` with one `OrderItem` per vendor line and one `Shipment` per vendor.

---

## How an order works

```
Customer clicks "Place Order"
  -> CheckoutController -> PlaceOrderCommand
  -> ValidationBehaviour (shipping address required)
  -> Handler:
       1. Verify the shipping address belongs to the customer
       2. Load cart (variants -> products -> stores -> vendors)
       3. Re-validate stock and availability
       4. Build Order with an address snapshot
       5. For each cart item: create an OrderItem snapshot, compute commission, decrement stock
       6. Apply coupon if provided (window, usage caps, minimum order)
       7. Create the Payment record, clear the cart
       8. Raise OrderPlacedEvent, SaveChanges
            -> AuditableEntityInterceptor stamps audit fields
            -> DispatchDomainEventsInterceptor publishes OrderPlacedEvent
                 -> OrderPlacedAuditHandler writes an AuditLog entry
       9. Send confirmation email
  -> Result<Guid>(orderId) -> redirect to the order confirmation
```

Payment supports both a **mock provider** (always succeeds, for demos) and **Stripe Checkout** (when keys are configured in Admin -> Stripe settings or `appsettings.json`).

---

## Use cases

Every feature below maps to a MediatR handler in the Application layer and a controller action in the Web layer.

### Customer use cases

Customers are visitors who register and shop. Browsing is public; carts, checkout, and account features require login.

1. **Home & discovery** - landing page with up to 8 featured products and a category grid.
2. **Product browsing & search** - public, paginated grid (12/page) with keyword, category, store, and min/max price filters.
3. **Product detail page** - SEO-friendly slug URL, all images, active variants (color/size/price/stock), approved reviews, add-to-cart, add-to-wishlist, and an inline review form.
4. **Store pages** - public list of active stores and an individual store page (banner, logo, contact, product grid).
5. **Shopping cart** - add items (stock-validated), inline quantity updates, remove items, persistent DB-backed cart, and a live mini-cart count in the navbar.
6. **Checkout & order placement** - review summary, pick/add a shipping address, choose a payment method, optionally apply a coupon, and place the order atomically.
7. **Order history & tracking** - paginated order list, order detail with shipment tracking, cancel an eligible order, request a refund, and download a PDF invoice.
8. **Address book** - list, add, edit, delete addresses and mark a default for faster checkout.
9. **Wishlist** - save products for later, view them, and remove them (no duplicates per product).
10. **Product reviews** - submit a 1-5 star rating with optional title/comment; held for admin moderation, optionally tied to a verified purchase.
11. **User profile** - view profile, edit display name, upload an avatar, and change password.
12. **Notifications** - live unread bell count (SignalR), notification inbox, mark one or all as read.
13. **Language switching** - toggle English / Turkish; sets a culture cookie and re-localizes all UI text.
14. **Contact form** - send a message to platform support via `IEmailService`.

### Vendor use cases

Vendors are approved business accounts working in the **Vendor Area** (`/Vendor/...`). A vendor must be admin-approved before selling.

1. **Vendor dashboard** - KPI tiles (stores, products, pending fulfillment items, low-stock variants) and a 14-day sales chart.
2. **Store settings** - edit store name/description/contact and upload logo + banner; changes reflect on the public store page.
3. **Product catalog management** - paginated, filterable product list; create products with images and inline variants; edit/delete; toggle publish; a dedicated low-stock view (<= 5 units). New/edited products enter `Pending` approval.
4. **Order fulfillment** - filterable order list; per-order detail showing only the vendor's own items; advance each item through `PendingFulfillment -> Processing -> ReadyToShip -> Shipped -> Delivered/Cancelled`; create shipments with carrier and tracking.
5. **Shipment tracking** - filterable shipments list and a mark-delivered action.
6. **Customer insights** - repeat-buyer list aggregated from the vendor's own order history (order count, total spent, last order date).
7. **Review management** - read reviews for the vendor's products and post a public reply shown on the product page.
8. **Coupon management** - create/edit/delete store-scoped discount codes (percentage or fixed, with usage caps and date window) that apply only to the vendor's products.

### Admin use cases

Admins have full platform control through the **Admin Area** (`/Admin/...`).

1. **Admin dashboard** - KPI tiles (vendors, pending applications, products, pending approvals, orders, customers, revenue) and a 14-day revenue/order chart; revenue excludes cancelled orders.
2. **Vendor management** - review applications, approve / revoke selling rights, and set a vendor's commission percent (applies to future orders only).
3. **Product approval** - review submitted products and approve (publish) or reject them, with filters by status, name, store, and category.
4. **Category management** - create/edit/delete the hierarchical category tree (delete is blocked while products reference a category).
5. **User management** - search users, view profiles and roles, and reassign roles (Admin / Vendor / Customer / Delivery) via a diff operation.
6. **Review moderation** - approve reviews (making them public and counting toward product ratings) or keep them suppressed.
7. **Refund management** - review refund requests on delivered orders and approve (refunds the payment, marks the order refunded, notifies the customer) or reject.
8. **Coupon management (platform-wide)** - create/edit/delete discount codes that apply across all stores (`VendorStoreId = null`).
9. **Audit log** - paginated, filterable record (user, action type, date range) of every create/update/delete/login/logout, including IP address; audit entries are never deleted.

---

## Tech stack

| Concern | Choice |
|---|---|
| Framework | ASP.NET Core MVC 8.0 (C# 12) |
| ORM | EF Core 8 (Code-First) on SQL Server LocalDB |
| Mediator | MediatR 12 |
| Validation | FluentValidation 11 + DataAnnotations |
| Mapping | AutoMapper 14 |
| Auth | ASP.NET Core Identity (cookie-based, 14-day sliding) |
| Payments | Stripe (Stripe.net) + a mock provider |
| UI | Razor Views + Bootstrap 5 + AdminLTE 4 + Bootstrap Icons |
| Charts | Chart.js |
| PDF | QuestPDF (order invoices) |
| Real-time | ASP.NET Core SignalR |
| Localization | `IStringLocalizer` + `.resx` (en / tr) |
| Email | `IEmailService` -> `ConsoleEmailService` (logs in dev; swap for SMTP in production) |
| Tests | xUnit + EF InMemory + FluentAssertions + Moq |

---

## Project layout

```
MVE.slnx
|
+-- src/
|   +-- MVE.Domain/          Entities, enums, base classes, domain events
|   +-- MVE.Application/     CQRS handlers, DTOs, validators, interfaces
|   +-- MVE.Infrastructure/  DbContext, Identity, services, interceptors, migrations, seed
|   +-- MVE.Web/             Controllers, Views, ViewModels, Areas (Admin / Vendor), SignalR hub
|
+-- tests/
    +-- MVE.Application.UnitTests/   xUnit tests (PlaceOrder, CreateReview)
```

---

## Configuration

`src/MVE.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=(localdb)\\mssqllocaldb;Database=MVEDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Stripe": {
    "SecretKey": "",
    "PublishableKey": "",
    "Currency": "usd"
  }
}
```

- The app calls `MigrateAsync()` + `DbInitializer` on startup, so the database is created and seeded automatically.
- Stripe keys can be set here or, at runtime, in **Admin -> Stripe settings** (DB values take precedence). With no keys, checkout falls back to the mock payment provider.

---

## Running the tests

```bash
dotnet test
```

```bash
# A single test class
dotnet test --filter "FullyQualifiedName~PlaceOrderCommandHandlerTests"
```

---

## License

Academic project - provided as-is, with no production warranty.
