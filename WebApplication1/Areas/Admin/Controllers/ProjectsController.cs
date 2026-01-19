using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PortfolioWeb.Areas.Admin.Models;
using PortfolioWeb.Data;
using PortfolioWeb.Models;

namespace PortfolioWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,User")]
public class ProjectsController : Controller
{
    private readonly PortfolioRepository _repo;
    private readonly ILogger<ProjectsController> _logger;
    private readonly IWebHostEnvironment _env;

    private const string PortfolioUserIdClaim = "PortfolioUserId";

    public ProjectsController(PortfolioRepository repo, ILogger<ProjectsController> logger, IWebHostEnvironment env)
    {
        _repo = repo;
        _logger = logger;
        _env = env;
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
        var rows = await _repo.GetProjectsAllAsync(contextUserId);
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
        return View(new ProjectEditModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectEditModel input, List<IFormFile>? ImageFiles, int? userId = null)
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
            var uploaded = await SaveUploadedImagesAsync(ImageFiles);
            var mergedImages = MergeImageInputs(input.ImagesText, uploaded);

            var row = new ProjectRecord
            {
                UserId = contextUserId is null ? null : (uint)contextUserId.Value,
                Title = input.Title?.Trim() ?? string.Empty,
                Description = input.Description ?? string.Empty,
                Images = mergedImages,
                ProjectLink = string.IsNullOrWhiteSpace(input.ProjectLink) ? null : input.ProjectLink.Trim(),
                TechStack = NormalizeLinesToJsonArray(input.TechStackText),
                DisplayOrder = input.DisplayOrder,
                IsActive = input.IsActive ? 1 : 0,
            };

            await _repo.InsertProjectAsync(row);
            return RedirectToAction("Index", new { userId = User.IsInRole("Admin") ? contextUserId : null });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Project validation failed.");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(input);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project.");
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

        var row = await _repo.GetProjectByIdAsync(id);
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

        var model = new ProjectEditModel
        {
            Id = row.Id,
            Title = row.Title,
            Description = row.Description,
            ImagesText = JsonArrayToLines(row.Images),
            ProjectLink = row.ProjectLink,
            TechStackText = JsonArrayToLines(row.TechStack),
            DisplayOrder = row.DisplayOrder,
            IsActive = row.IsActive == 1,
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectEditModel input, List<IFormFile>? ImageFiles, int? userId = null)
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
            var existing = await _repo.GetProjectByIdAsync((int)input.Id);
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

            var uploaded = await SaveUploadedImagesAsync(ImageFiles);
            var mergedImages = MergeImageInputs(input.ImagesText, uploaded);

            var row = new ProjectRecord
            {
                Id = input.Id,
                Title = input.Title?.Trim() ?? string.Empty,
                Description = input.Description ?? string.Empty,
                Images = mergedImages,
                ProjectLink = string.IsNullOrWhiteSpace(input.ProjectLink) ? null : input.ProjectLink.Trim(),
                TechStack = NormalizeLinesToJsonArray(input.TechStackText),
                DisplayOrder = input.DisplayOrder,
                IsActive = input.IsActive ? 1 : 0,
            };

            await _repo.UpdateProjectAsync(row);
            return RedirectToAction("Index", new { userId = User.IsInRole("Admin") ? (existing.UserId.HasValue ? (int)existing.UserId.Value : contextUserId) : null });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Project validation failed.");
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(input);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update project.");
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

        var existing = await _repo.GetProjectByIdAsync(id);
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

        await _repo.DeleteProjectAsync(id);
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

    private async Task<List<string>> SaveUploadedImagesAsync(List<IFormFile>? files)
    {
        var result = new List<string>();
        if (files is null || files.Count == 0)
        {
            return result;
        }

        var uploadsPath = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsPath);

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg" };

        foreach (var file in files)
        {
            if (file is null || file.Length <= 0)
            {
                continue;
            }

            var ext = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                throw new InvalidOperationException("Unsupported image type.");
            }

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsPath, fileName);

            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream);
            result.Add(fileName);
        }

        return result;
    }

    private static string? MergeImageInputs(string? imagesText, List<string> uploaded)
    {
        var fromText = (imagesText ?? string.Empty)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Path.GetFileName((s ?? string.Empty).Trim()))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var merged = fromText
            .Concat(uploaded.Select(s => Path.GetFileName((s ?? string.Empty).Trim())))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (merged.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(merged);
    }
}
