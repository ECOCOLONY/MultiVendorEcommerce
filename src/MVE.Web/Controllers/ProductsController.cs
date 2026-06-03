using Application.Categories.Queries.GetCategoryLookup;
using Application.Products.Queries.GetPublicProductBySlug;
using Application.Products.Queries.GetPublicProducts;
using Application.Reviews.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

public class ProductsController : Controller
{
    private readonly ISender _mediator;
    public ProductsController(ISender mediator) => _mediator = mediator;

    public async Task<IActionResult> Index(string? search = null, Guid? categoryId = null, string? store = null,
        decimal? minPrice = null, decimal? maxPrice = null, int page = 1)
    {
        var list = await _mediator.Send(new GetPublicProductsQuery(search, categoryId, store, minPrice, maxPrice, page, 12));
        ViewBag.Categories = await _mediator.Send(new GetCategoryLookupQuery());
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.StoreSlug = store;
        return View(list);
    }

    [HttpGet("/Products/Details/{slug}")]
    public async Task<IActionResult> Details(string slug)
    {
        var p = await _mediator.Send(new GetPublicProductBySlugQuery(slug));
        ViewBag.Reviews = await _mediator.Send(new GetPublicReviewsQuery(p.Id));
        return View(p);
    }
}
