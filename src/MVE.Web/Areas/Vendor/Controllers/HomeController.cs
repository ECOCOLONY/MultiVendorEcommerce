using Application.Common.Interfaces;
using Application.Vendors.Queries.GetMyVendor;
using Application.VendorOrders.Queries.GetVendorSalesChart;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Web.Areas.Vendor.Controllers;

[Area("Vendor")]
[Authorize(Roles = "Vendor")]
public class HomeController : Controller
{
    private readonly ISender _mediator;
    private readonly IApplicationDbContext _db;

    public HomeController(ISender mediator, IApplicationDbContext db)
    {
        _mediator = mediator;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var me = await _mediator.Send(new GetMyVendorQuery());
        if (me is null) return Forbid();

        var storeIds = me.Stores.Select(s => s.Id).ToArray();

        ViewData["StoreCount"]      = me.Stores.Count;
        ViewData["ProductCount"]    = await _db.Products.CountAsync(p => storeIds.Contains(p.VendorStoreId));
        ViewData["PendingOrders"]   = await _db.OrderItems.CountAsync(i => storeIds.Contains(i.VendorStoreId)
                                                                          && (int)i.VendorFulfillmentStatus < 3);
        ViewData["LowStock"]        = await _db.ProductVariants.CountAsync(v => v.Product != null
                                                                                && storeIds.Contains(v.Product.VendorStoreId)
                                                                                && v.StockQuantity <= 5);

        ViewBag.SalesChart = await _mediator.Send(new GetVendorSalesChartQuery(14));
        return View(me);
    }
}
