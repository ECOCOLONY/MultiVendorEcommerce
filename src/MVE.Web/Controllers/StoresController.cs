using Application.Products.Queries.GetPublicProducts;
using Application.VendorStores.Queries.GetPublicStoreBySlug;
using Application.VendorStores.Queries.GetPublicStores;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class StoresController : Controller
{
    private readonly ISender _mediator;
    public StoresController(ISender mediator) => _mediator = mediator;

    [HttpGet("/Stores")]
    public async Task<IActionResult> List() =>
        View(await _mediator.Send(new GetPublicStoresQuery()));

    [HttpGet("/Stores/{slug}")]
    public async Task<IActionResult> Index(string slug, int page = 1)
    {
        var store = await _mediator.Send(new GetPublicStoreBySlugQuery(slug));
        var products = await _mediator.Send(new GetPublicProductsQuery(StoreSlug: slug, Page: page, PageSize: 12));
        ViewBag.Products = products;
        return View(store);
    }
}
