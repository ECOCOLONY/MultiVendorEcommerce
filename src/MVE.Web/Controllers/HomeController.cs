using System.Diagnostics;
using Application.Categories.Queries.GetCategoryLookup;
using Application.Common.Interfaces;
using Application.Products.Queries.GetFeaturedProducts;
using Application.Settings.Queries.GetPublicWebsiteSettings;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Web.Models;
using Web.Models.ViewModels.Home;

namespace Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ISender _mediator;
    private readonly IEmailService _email;

    public HomeController(ILogger<HomeController> logger, ISender mediator, IEmailService email)
    {
        _logger = logger;
        _mediator = mediator;
        _email = email;
    }

    public async Task<IActionResult> Index()
    {
        var featured   = await _mediator.Send(new GetFeaturedProductsQuery(8));
        var categories = await _mediator.Send(new GetCategoryLookupQuery());
        ViewBag.Featured   = featured;
        ViewBag.Categories = categories;
        return View();
    }

    public IActionResult About()   => View();
    public IActionResult Faq()     => View();
    public IActionResult Terms()   => View();
    public IActionResult Privacy() => View();

    [HttpGet]
    public async Task<IActionResult> Contact()
    {
        ViewBag.Site = await _mediator.Send(new GetPublicWebsiteSettingsQuery());
        return View(new ContactViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel vm)
    {
        var site = await _mediator.Send(new GetPublicWebsiteSettingsQuery());
        ViewBag.Site = site;

        if (!ModelState.IsValid) return View(vm);

        var to = site.EffectiveContactRecipient;
        if (string.IsNullOrEmpty(to))
        {
            ModelState.AddModelError(string.Empty, "Contact email is not configured. Ask an administrator to set it under Admin → Website & SEO.");
            return View(vm);
        }

        var body = $"<p><strong>From:</strong> {vm.Name} &lt;{vm.Email}&gt;</p>" +
                   $"<p><strong>Subject:</strong> {vm.Subject}</p>" +
                   $"<hr/><p>{System.Net.WebUtility.HtmlEncode(vm.Message).Replace("\n", "<br/>")}</p>";
        await _email.SendAsync(to, $"[Contact form] {vm.Subject}", body);

        TempData["Success"] = "Thanks! We received your message and will get back to you soon.";
        return RedirectToAction(nameof(Contact));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
