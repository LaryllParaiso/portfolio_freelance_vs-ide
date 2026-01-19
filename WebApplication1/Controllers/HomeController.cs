using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PortfolioWeb.Data;
using PortfolioWeb.Models;

namespace PortfolioWeb.Controllers;

public class HomeController : Controller
{
    private readonly PortfolioRepository _repo;
    private readonly ILogger<HomeController> _logger;

    private const string PortfolioUserIdClaim = "PortfolioUserId";

    public HomeController(PortfolioRepository repo, ILogger<HomeController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? userId = null)
    {
        int? portfolioUserId = null;

        if (User?.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("User"))
            {
                var claim = User.FindFirstValue(PortfolioUserIdClaim);
                if (int.TryParse(claim, out var parsed) && parsed > 0)
                {
                    portfolioUserId = parsed;
                }
            }
            else if (User.IsInRole("Admin") && userId is not null && userId.Value > 0)
            {
                portfolioUserId = userId.Value;
            }
        }

        var vm = await _repo.GetHomePageAsync(portfolioUserId);
        return View(vm);
    }

    [HttpGet("/home")]
    public IActionResult Home()
    {
        return Redirect(Url.Action("Index", "Home") + "#home");
    }

    [HttpGet("/about")]
    public IActionResult About()
    {
        return Redirect(Url.Action("Index", "Home") + "#about");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactFormInput input)
    {
        if (!ModelState.IsValid)
        {
            SetContactTempData(input);
            return Redirect(Url.Action("Index", "Home") + "#contact");
        }

        try
        {
            await _repo.InsertContactMessageAsync(input);
            TempData["ContactSuccess"] = "Message sent successfully.";
            return Redirect(Url.Action("Index", "Home") + "#contact");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Contact form submission failed.");

            TempData["ContactErrors"] = JsonSerializer.Serialize(new List<string>
            {
                "Sorry, something went wrong sending your message. Please try again."
            });

            TempData["ContactOldName"] = input.Name ?? string.Empty;
            TempData["ContactOldEmail"] = input.Email ?? string.Empty;
            TempData["ContactOldMessage"] = input.Message ?? string.Empty;

            return Redirect(Url.Action("Index", "Home") + "#contact");
        }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private void SetContactTempData(ContactFormInput input)
    {
        var errors = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToList();

        TempData["ContactErrors"] = JsonSerializer.Serialize(errors);
        TempData["ContactOldName"] = input.Name ?? string.Empty;
        TempData["ContactOldEmail"] = input.Email ?? string.Empty;
        TempData["ContactOldMessage"] = input.Message ?? string.Empty;
    }
}
