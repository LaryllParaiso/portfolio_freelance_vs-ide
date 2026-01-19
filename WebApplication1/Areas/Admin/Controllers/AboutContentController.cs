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
public class AboutContentController : Controller
{
    private readonly PortfolioRepository _repo;
    private readonly ILogger<AboutContentController> _logger;
    private readonly IWebHostEnvironment _env;

    private const string PortfolioUserIdClaim = "PortfolioUserId";

    public AboutContentController(PortfolioRepository repo, ILogger<AboutContentController> logger, IWebHostEnvironment env)
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
        var rows = await _repo.GetAboutContentAllAsync(contextUserId);
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
        return View(new AboutContentEditModel
        {
            ExperienceItems = new List<AboutExperienceItem> { new AboutExperienceItem() }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AboutContentEditModel input, IFormFile? ProfileImageFile, int? userId = null)
    {
        var contextUserId = ResolveContextUserId(userId);
        if (User.IsInRole("User") && contextUserId is null)
        {
            return Forbid();
        }

        ViewData["UserId"] = contextUserId;

        if (!ModelState.IsValid)
        {
            EnsureExperienceRows(input);
            return View(input);
        }

        try
        {
            var profileImage = await SaveUploadedImageAsync(ProfileImageFile) ?? NormalizeStoredImage(input.ProfileImage);
            var record = new AboutContentRecord
            {
                UserId = contextUserId is null ? null : (uint)contextUserId.Value,
                Bio = input.Bio ?? string.Empty,
                ProfileImage = profileImage,
                Skills = NormalizeLinesToJsonArray(input.SkillsText),
                Experience = SerializeExperience(input.ExperienceItems) ?? (string.IsNullOrWhiteSpace(input.ExperienceJson) ? null : input.ExperienceJson.Trim()),
                IsActive = input.IsActive ? 1 : 0,
            };

            await _repo.InsertAboutContentAsync(record);
            return RedirectToAction("Index", new { userId = User.IsInRole("Admin") ? contextUserId : null });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "About content validation failed.");
            ModelState.AddModelError(string.Empty, ex.Message);
            EnsureExperienceRows(input);
            return View(input);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create about content.");
            ModelState.AddModelError(string.Empty, "Unable to save right now.");
            EnsureExperienceRows(input);
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

        var row = await _repo.GetAboutContentByIdAsync(id);
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

        var model = new AboutContentEditModel
        {
            Id = row.Id,
            Bio = row.Bio,
            ProfileImage = row.ProfileImage,
            SkillsText = JsonArrayToLines(row.Skills),
            ExperienceJson = row.Experience,
            ExperienceItems = DeserializeExperience(row.Experience),
            IsActive = row.IsActive == 1,
        };

        EnsureExperienceRows(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AboutContentEditModel input, IFormFile? ProfileImageFile, int? userId = null)
    {
        var contextUserId = ResolveContextUserId(userId);
        if (User.IsInRole("User") && contextUserId is null)
        {
            return Forbid();
        }

        ViewData["UserId"] = contextUserId;

        if (!ModelState.IsValid)
        {
            EnsureExperienceRows(input);
            return View(input);
        }

        try
        {
            var existing = await _repo.GetAboutContentByIdAsync((int)input.Id);
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

            var profileImage = await SaveUploadedImageAsync(ProfileImageFile) ?? NormalizeStoredImage(input.ProfileImage);
            var row = new AboutContentRecord
            {
                Id = input.Id,
                Bio = input.Bio ?? string.Empty,
                ProfileImage = profileImage,
                Skills = NormalizeLinesToJsonArray(input.SkillsText),
                Experience = SerializeExperience(input.ExperienceItems) ?? (string.IsNullOrWhiteSpace(input.ExperienceJson) ? null : input.ExperienceJson.Trim()),
                IsActive = input.IsActive ? 1 : 0,
            };

            await _repo.UpdateAboutContentAsync(row);
            return RedirectToAction("Index", new { userId = User.IsInRole("Admin") ? (existing.UserId.HasValue ? (int)existing.UserId.Value : contextUserId) : null });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "About content validation failed.");
            ModelState.AddModelError(string.Empty, ex.Message);
            EnsureExperienceRows(input);
            return View(input);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update about content.");
            ModelState.AddModelError(string.Empty, "Unable to save right now.");
            EnsureExperienceRows(input);
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

        var existing = await _repo.GetAboutContentByIdAsync(id);
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

        await _repo.DeleteAboutContentAsync(id);
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

    private async Task<string?> SaveUploadedImageAsync(IFormFile? file)
    {
        if (file is null || file.Length <= 0)
        {
            return null;
        }

        var ext = Path.GetExtension(file.FileName ?? string.Empty).ToLowerInvariant();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg" };
        if (!allowed.Contains(ext))
        {
            throw new InvalidOperationException("Unsupported image type.");
        }

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var uploadsPath = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(uploadsPath);
        var fullPath = Path.Combine(uploadsPath, fileName);

        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);
        return fileName;
    }

    private static string? NormalizeStoredImage(string? input)
    {
        var file = Path.GetFileName((input ?? string.Empty).Trim());
        return string.IsNullOrWhiteSpace(file) ? null : file;
    }

    private static void EnsureExperienceRows(AboutContentEditModel model)
    {
        model.ExperienceItems ??= new List<AboutExperienceItem>();
        if (model.ExperienceItems.Count == 0)
        {
            model.ExperienceItems.Add(new AboutExperienceItem());
        }
    }

    private static List<AboutExperienceItem> DeserializeExperience(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<AboutExperienceItem>();
        }

        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<AboutExperienceItem>>(json, opts) ?? new List<AboutExperienceItem>();
        }
        catch
        {
            return new List<AboutExperienceItem>();
        }
    }

    private static string? SerializeExperience(List<AboutExperienceItem>? items)
    {
        if (items is null || items.Count == 0)
        {
            return null;
        }

        var cleaned = items
            .Select(i => new AboutExperienceItem
            {
                Year = string.IsNullOrWhiteSpace(i.Year) ? null : i.Year.Trim(),
                Role = string.IsNullOrWhiteSpace(i.Role) ? null : i.Role.Trim(),
                Company = string.IsNullOrWhiteSpace(i.Company) ? null : i.Company.Trim(),
                Description = string.IsNullOrWhiteSpace(i.Description) ? null : i.Description.Trim(),
            })
            .Where(i => !string.IsNullOrWhiteSpace((i.Year ?? string.Empty) + (i.Role ?? string.Empty) + (i.Company ?? string.Empty) + (i.Description ?? string.Empty)))
            .ToList();

        return cleaned.Count == 0 ? null : JsonSerializer.Serialize(cleaned);
    }
}
