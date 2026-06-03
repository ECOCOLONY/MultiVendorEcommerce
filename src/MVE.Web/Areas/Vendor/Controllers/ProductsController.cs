using Application.Categories.Queries.GetCategoryLookup;
using Application.Common.Interfaces;
using Domain.Enums;
using Application.ProductImages.Commands;
using Application.Products.Commands.CreateProduct;
using Application.Products.Commands.DeleteProduct;
using Application.Products.Commands.UpdateProduct;
using Application.Products.Queries.GetMyProductById;
using Application.Products.Queries.GetMyProducts;
using Application.ProductVariants.Commands;
using Application.ProductVariants.Queries.GetLowStockVariants;
using Application.Vendors.Queries.GetMyVendor;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web.Models.ViewModels.Vendor.Products;

namespace Web.Areas.Vendor.Controllers;

[Area("Vendor")]
[Authorize(Roles = "Vendor")]
public class ProductsController : Controller
{
    private readonly ISender _mediator;
    private readonly IFileStorageService _files;

    public ProductsController(ISender mediator, IFileStorageService files)
    {
        _mediator = mediator;
        _files = files;
    }

    public async Task<IActionResult> Index(
        string? search = null,
        ProductApprovalStatus? approvalStatus = null,
        Guid? categoryId = null,
        int page = 1)
    {
        ViewBag.Search = search;
        ViewBag.ApprovalStatus = approvalStatus;
        ViewBag.CategoryId = categoryId;
        ViewBag.Categories = (await _mediator.Send(new GetCategoryLookupQuery()))
            .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(c.Name, c.Id.ToString()));
        var list = await _mediator.Send(new GetMyProductsQuery(search, approvalStatus, categoryId, page));
        return View(list);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var p = await _mediator.Send(new GetMyProductByIdQuery(id));
        return View(p);
    }

    public async Task<IActionResult> Create()
    {
        var me = await _mediator.Send(new GetMyVendorQuery());
        if (me is null) return Forbid();
        if (!me.IsApproved)
        {
            TempData["Error"] = "Your vendor account is not yet approved.";
            return RedirectToAction("Index", "Home");
        }

        var vm = new ProductCreateViewModel
        {
            VendorStoreId = me.Stores.FirstOrDefault()?.Id ?? Guid.Empty,
            Variants = new List<VariantInputViewModel> { new() { Price = 1m, StockQuantity = 0 } }
        };
        await PopulateLookups(vm, me);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateViewModel vm)
    {
        var me = await _mediator.Send(new GetMyVendorQuery());
        if (me is null) return Forbid();

        if (!ModelState.IsValid) { await PopulateLookups(vm, me); return View(vm); }

        var imageUrls = new List<string>();
        if (vm.ImageFiles is { Count: > 0 })
            imageUrls = await _files.SaveManyAsync(vm.ImageFiles.Where(f => f is { Length: > 0 }), "products");

        var cmd = new CreateProductCommand(
            vm.VendorStoreId, vm.CategoryId, vm.Name, vm.Description, vm.BasePrice,
            imageUrls,
            vm.Variants.Select(v => new CreateVariantInput(v.Sku, v.Name, v.Color, v.Size, v.Price, v.StockQuantity)).ToList());

        var result = await _mediator.Send(cmd);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);
            await PopulateLookups(vm, me);
            return View(vm);
        }

        TempData["Success"] = "Product created. It will appear publicly once an admin approves it.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var p = await _mediator.Send(new GetMyProductByIdQuery(id));
        var vm = new ProductEditViewModel
        {
            Id = p.Id, CategoryId = p.CategoryId, Name = p.Name, Description = p.Description,
            BasePrice = p.BasePrice, IsPublished = p.IsPublished, StoreName = p.StoreName, Slug = p.Slug,
            ApprovalStatus = p.ApprovalStatus.ToString()
        };
        await PopulateCategories(vm);
        ViewBag.Detail = p;
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductEditViewModel vm)
    {
        if (!ModelState.IsValid) { await PopulateCategories(vm); return View(vm); }

        var result = await _mediator.Send(new UpdateProductCommand(
            vm.Id, vm.CategoryId, vm.Name, vm.Description, vm.BasePrice, vm.IsPublished));

        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e);
            await PopulateCategories(vm);
            return View(vm);
        }

        TempData["Success"] = "Product updated. Awaiting re-approval.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var p = await _mediator.Send(new GetMyProductByIdQuery(id));
        return View(p);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var r = await _mediator.Send(new DeleteProductCommand(id));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Product deleted." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Index));
    }

    // -------- Image management --------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddImage(Guid id, IFormFile imageFile)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            TempData["Error"] = "Please choose an image.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        var url = await _files.SaveAsync(imageFile, "products");
        var r = await _mediator.Send(new AddProductImageCommand(id, url));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Image added." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId)
    {
        var r = await _mediator.Send(new DeleteProductImageCommand(imageId));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Image removed." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMainImage(Guid id, Guid imageId)
    {
        var r = await _mediator.Send(new SetMainProductImageCommand(imageId));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Main image updated." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Edit), new { id });
    }

    // -------- Variant management --------

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddVariant(Guid id, string sku, string? name, string? color, string? size, decimal price, int stockQuantity)
    {
        var r = await _mediator.Send(new AddProductVariantCommand(id, sku, name, color, size, price, stockQuantity));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Variant added." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateVariant(Guid id, Guid variantId, string? name, string? color, string? size,
        decimal price, int stockQuantity, bool isActive)
    {
        var r = await _mediator.Send(new UpdateProductVariantCommand(variantId, name, color, size, price, stockQuantity, isActive));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Variant updated." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVariant(Guid id, Guid variantId)
    {
        var r = await _mediator.Send(new DeleteProductVariantCommand(variantId));
        TempData[r.Succeeded ? "Success" : "Error"] = r.Succeeded ? "Variant deleted." : string.Join(" ", r.Errors);
        return RedirectToAction(nameof(Edit), new { id });
    }

    // -------- Low stock --------

    public async Task<IActionResult> LowStock(int threshold = 5)
    {
        ViewBag.Threshold = threshold;
        return View(await _mediator.Send(new GetLowStockVariantsQuery(threshold)));
    }

    private async Task PopulateLookups(ProductCreateViewModel vm, Application.Vendors.Queries.GetMyVendor.MyVendorDto me)
    {
        vm.Stores = me.Stores.Select(s => new SelectListItem(s.Name, s.Id.ToString()));
        vm.Categories = (await _mediator.Send(new GetCategoryLookupQuery()))
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()));
    }

    private async Task PopulateCategories(ProductEditViewModel vm)
    {
        vm.Categories = (await _mediator.Send(new GetCategoryLookupQuery()))
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()));
    }
}
