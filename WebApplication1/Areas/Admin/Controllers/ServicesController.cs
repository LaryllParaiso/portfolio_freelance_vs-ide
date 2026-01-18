using System;
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
public class ServicesController : Controller
{
    private readonly PortfolioRepository _repo;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(PortfolioRepository repo, ILogger<ServicesController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rows = await _repo.GetServicesAllAsync();
        return View(rows);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ServiceEditModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceEditModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            var row = new ServiceRecord
            {
                Title = input.Title?.Trim() ?? string.Empty,
                Description = input.Description ?? string.Empty,
                Pricing = string.IsNullOrWhiteSpace(input.Pricing) ? null : input.Pricing.Trim(),
                Tags = string.IsNullOrWhiteSpace(input.Tags) ? null : input.Tags.Trim(),
                DisplayOrder = input.DisplayOrder,
                IsActive = input.IsActive ? 1 : 0,
            };

            await _repo.InsertServiceAsync(row);
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create service.");
            ModelState.AddModelError(string.Empty, "Unable to save right now.");
            return View(input);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var row = await _repo.GetServiceByIdAsync(id);
        if (row is null)
        {
            return NotFound();
        }

        var model = new ServiceEditModel
        {
            Id = row.Id,
            Title = row.Title,
            Description = row.Description,
            Pricing = row.Pricing,
            Tags = row.Tags,
            DisplayOrder = row.DisplayOrder,
            IsActive = row.IsActive == 1,
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ServiceEditModel input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            var row = new ServiceRecord
            {
                Id = input.Id,
                Title = input.Title?.Trim() ?? string.Empty,
                Description = input.Description ?? string.Empty,
                Pricing = string.IsNullOrWhiteSpace(input.Pricing) ? null : input.Pricing.Trim(),
                Tags = string.IsNullOrWhiteSpace(input.Tags) ? null : input.Tags.Trim(),
                DisplayOrder = input.DisplayOrder,
                IsActive = input.IsActive ? 1 : 0,
            };

            await _repo.UpdateServiceAsync(row);
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update service.");
            ModelState.AddModelError(string.Empty, "Unable to save right now.");
            return View(input);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _repo.DeleteServiceAsync(id);
        return RedirectToAction("Index");
    }
}
