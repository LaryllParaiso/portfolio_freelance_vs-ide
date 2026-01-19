using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PortfolioWeb.Areas.Admin.Models;
using PortfolioWeb.Data;
using PortfolioWeb.Models;

namespace PortfolioWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,User")]
public class HomeContentController : Controller
{
    private readonly PortfolioRepository _repo;
    private readonly ILogger<HomeContentController> _logger;

    private const string PortfolioUserIdClaim = "PortfolioUserId";

    public HomeContentController(PortfolioRepository repo, ILogger<HomeContentController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? userId = null)
    {
        var contextUserId = ResolveContextUserId(userId);
        if (User.IsInRole("User") && contextUserId is null)
        {
            return Forbid();
        }

        ViewData["UserId"] = contextUserId;
        var rows = await _repo.GetHomeContentAllAsync(contextUserId);
        return View(rows);
    }

    [HttpGet]
    public IActionResult Create(int? userId = null)
    {
        var contextUserId = ResolveContextUserId(userId);
        if (User.IsInRole("User") && contextUserId is null)
        {
            return Forbid();
        }

        ViewData["UserId"] = contextUserId;
        return View(new HomeContentEditModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HomeContentEditModel input, int? userId = null)
    {
        var contextUserId = ResolveContextUserId(userId);
        if (User.IsInRole("User") && contextUserId is null)
        {
            return Forbid();
        }

        ViewData["UserId"] = contextUserId;

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            var record = new HomeContentRecord
            {
                UserId = contextUserId is null ? null : (uint)contextUserId.Value,
                HeroTitle = input.HeroTitle?.Trim() ?? string.Empty,
                HeroSubtitle = input.HeroSubtitle?.Trim() ?? string.Empty,
                HeroCtaText = input.HeroCtaText?.Trim() ?? string.Empty,
                HeroCtaLink = input.HeroCtaLink?.Trim() ?? string.Empty,
                IsActive = input.IsActive ? 1 : 0,
            };

            await _repo.InsertHomeContentAsync(record);
            return RedirectToAction("Index", new { userId = User.IsInRole("Admin") ? contextUserId : null });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create home content.");
            ModelState.AddModelError(string.Empty, "Unable to save right now.");
            return View(input);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, int? userId = null)
    {
        var contextUserId = ResolveContextUserId(userId);
        if (User.IsInRole("User") && contextUserId is null)
        {
            return Forbid();
        }

        var row = await _repo.GetHomeContentByIdAsync(id);
        if (row is null)
        {
            return NotFound();
        }

        if (User.IsInRole("User"))
        {
            var owner = row.UserId.HasValue ? (int)row.UserId.Value : (int?)null;
            if (owner != contextUserId)
            {
                return Forbid();
            }
        }

        ViewData["UserId"] = User.IsInRole("Admin") ? (row.UserId.HasValue ? (int)row.UserId.Value : contextUserId) : contextUserId;

        var model = new HomeContentEditModel
        {
            Id = row.Id,
            HeroTitle = row.HeroTitle,
            HeroSubtitle = row.HeroSubtitle,
            HeroCtaText = row.HeroCtaText,
            HeroCtaLink = row.HeroCtaLink,
            IsActive = row.IsActive == 1,
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(HomeContentEditModel input, int? userId = null)
    {
        var contextUserId = ResolveContextUserId(userId);
        if (User.IsInRole("User") && contextUserId is null)
        {
            return Forbid();
        }

        ViewData["UserId"] = contextUserId;

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            var existing = await _repo.GetHomeContentByIdAsync((int)input.Id);
            if (existing is null)
            {
                return NotFound();
            }

            if (User.IsInRole("User"))
            {
                var owner = existing.UserId.HasValue ? (int)existing.UserId.Value : (int?)null;
                if (owner != contextUserId)
                {
                    return Forbid();
                }
            }

            var row = new HomeContentRecord
            {
                Id = input.Id,
                HeroTitle = input.HeroTitle?.Trim() ?? string.Empty,
                HeroSubtitle = input.HeroSubtitle?.Trim() ?? string.Empty,
                HeroCtaText = input.HeroCtaText?.Trim() ?? string.Empty,
                HeroCtaLink = input.HeroCtaLink?.Trim() ?? string.Empty,
                IsActive = input.IsActive ? 1 : 0,
            };

            await _repo.UpdateHomeContentAsync(row);
            return RedirectToAction("Index", new { userId = User.IsInRole("Admin") ? (existing.UserId.HasValue ? (int)existing.UserId.Value : contextUserId) : null });
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
    public async Task<IActionResult> Delete(int id, int? userId = null)
    {
        var contextUserId = ResolveContextUserId(userId);
        if (User.IsInRole("User") && contextUserId is null)
        {
            return Forbid();
        }

        var existing = await _repo.GetHomeContentByIdAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        if (User.IsInRole("User"))
        {
            var owner = existing.UserId.HasValue ? (int)existing.UserId.Value : (int?)null;
            if (owner != contextUserId)
            {
                return Forbid();
            }
        }

        await _repo.DeleteHomeContentAsync(id);
        return RedirectToAction("Index", new { userId = User.IsInRole("Admin") ? (existing.UserId.HasValue ? (int)existing.UserId.Value : contextUserId) : null });
    }

    private int? ResolveContextUserId(int? userId)
    {
        if (User.IsInRole("User"))
        {
            var claim = User.FindFirstValue(PortfolioUserIdClaim);
            return int.TryParse(claim, out var parsed) && parsed > 0 ? parsed : null;
        }

        if (User.IsInRole("Admin"))
        {
            return userId is not null && userId.Value > 0 ? userId.Value : null;
        }

        return null;
    }
}
