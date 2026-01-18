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
public class AboutContentController : Controller
{
    private readonly PortfolioRepository _repo;
    private readonly ILogger<AboutContentController> _logger;

    public AboutContentController(PortfolioRepository repo, ILogger<AboutContentController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rows = await _repo.GetAboutContentAllAsync();
        return View(rows);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new AboutContentEditModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AboutContentEditModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            var record = new AboutContentRecord
            {
                Bio = input.Bio ?? string.Empty,
                ProfileImage = string.IsNullOrWhiteSpace(input.ProfileImage) ? null : input.ProfileImage.Trim(),
                Skills = NormalizeLinesToJsonArray(input.SkillsText),
                Experience = string.IsNullOrWhiteSpace(input.ExperienceJson) ? null : input.ExperienceJson.Trim(),
                IsActive = input.IsActive ? 1 : 0,
            };

            await _repo.InsertAboutContentAsync(record);
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create about content.");
            ModelState.AddModelError(string.Empty, "Unable to save right now.");
            return View(input);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var row = await _repo.GetAboutContentByIdAsync(id);
        if (row is null)
        {
            return NotFound();
        }

        var model = new AboutContentEditModel
        {
            Id = row.Id,
            Bio = row.Bio,
            ProfileImage = row.ProfileImage,
            SkillsText = JsonArrayToLines(row.Skills),
            ExperienceJson = row.Experience,
            IsActive = row.IsActive == 1,
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AboutContentEditModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            var row = new AboutContentRecord
            {
                Id = input.Id,
                Bio = input.Bio ?? string.Empty,
                ProfileImage = string.IsNullOrWhiteSpace(input.ProfileImage) ? null : input.ProfileImage.Trim(),
                Skills = NormalizeLinesToJsonArray(input.SkillsText),
                Experience = string.IsNullOrWhiteSpace(input.ExperienceJson) ? null : input.ExperienceJson.Trim(),
                IsActive = input.IsActive ? 1 : 0,
            };

            await _repo.UpdateAboutContentAsync(row);
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update about content.");
            ModelState.AddModelError(string.Empty, "Unable to save right now.");
            return View(input);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _repo.DeleteAboutContentAsync(id);
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
