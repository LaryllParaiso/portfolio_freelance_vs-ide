using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PortfolioWeb.Data;

namespace PortfolioWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MessagesController : Controller
{
    private readonly PortfolioRepository _repo;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(PortfolioRepository repo, ILogger<MessagesController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rows = await _repo.GetContactMessagesAsync();
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var row = await _repo.GetContactMessageByIdAsync(id);
        if (row is null)
        {
            return NotFound();
        }

        return View(row);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _repo.DeleteContactMessageAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete contact message {MessageId}", id);
        }

        return RedirectToAction("Index");
    }
}
