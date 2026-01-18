using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PortfolioWeb.Areas.Admin.Models;
using PortfolioWeb.Data;
using PortfolioWeb.Models;

namespace PortfolioWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class HomeContentController : Controller
{
    private readonly PortfolioRepository _repo;
    private readonly ILogger<HomeContentController> _logger;

    public HomeContentController(PortfolioRepository repo, ILogger<HomeContentController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rows = await _repo.GetHomeContentAllAsync();
        return View(rows);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new HomeContentEditModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HomeContentEditModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            var record = new HomeContentRecord
            {
                HeroTitle = input.HeroTitle?.Trim() ?? string.Empty,
                HeroSubtitle = input.HeroSubtitle?.Trim() ?? string.Empty,
                HeroCtaText = input.HeroCtaText?.Trim() ?? string.Empty,
                HeroCtaLink = input.HeroCtaLink?.Trim() ?? string.Empty,
                Highlights = NormalizeLinesToJsonArray(input.HighlightsText),
                IsActive = input.IsActive ? 1 : 0,
            };

            await _repo.InsertHomeContentAsync(record);
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create home content.");
            ModelState.AddModelError(string.Empty, "Unable to save right now.");
            return View(input);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var row = await _repo.GetHomeContentByIdAsync(id);
        if (row is null)
        {
            return NotFound();
        }

        var model = new HomeContentEditModel
        {
            Id = row.Id,
            HeroTitle = row.HeroTitle,
            HeroSubtitle = row.HeroSubtitle,
            HeroCtaText = row.HeroCtaText,
            HeroCtaLink = row.HeroCtaLink,
            HighlightsText = JsonArrayToLines(row.Highlights),
            IsActive = row.IsActive == 1,
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(HomeContentEditModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            var row = new HomeContentRecord
            {
                Id = input.Id,
                HeroTitle = input.HeroTitle?.Trim() ?? string.Empty,
                HeroSubtitle = input.HeroSubtitle?.Trim() ?? string.Empty,
                HeroCtaText = input.HeroCtaText?.Trim() ?? string.Empty,
                HeroCtaLink = input.HeroCtaLink?.Trim() ?? string.Empty,
                Highlights = NormalizeLinesToJsonArray(input.HighlightsText),
                IsActive = input.IsActive ? 1 : 0,
            };

            await _repo.UpdateHomeContentAsync(row);
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update home content.");
            ModelState.AddModelError(string.Empty, "Unable to save right now.");
            return View(input);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _repo.DeleteHomeContentAsync(id);
        return RedirectToAction("Index");
    }

    private static string? NormalizeLinesToJsonArray(string? text)
    {
        var raw = (text ?? string.Empty).Trim();
        if (raw == string.Empty)
        {
            return null;
        }

        if (raw.StartsWith("["))
        {
            return raw;
        }

        var items = raw
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => (s ?? string.Empty).Trim())
            .Where(s => s != string.Empty)
            .ToList();

        if (items.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(items);
    }

    private static string? JsonArrayToLines(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            return string.Join(Environment.NewLine, list.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));
        }
        catch
        {
            return json;
        }
    }
}
