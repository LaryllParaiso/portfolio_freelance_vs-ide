using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PortfolioWeb.Areas.Admin.Models;
using PortfolioWeb.Data;

namespace PortfolioWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,User")]
public class DashboardController : Controller
{
    private readonly PortfolioRepository _repo;

    private const string PortfolioUserIdClaim = "PortfolioUserId";

    public DashboardController(PortfolioRepository repo)
    {
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? userId = null)
    {
        int? contextUserId = null;
        if (User.IsInRole("User"))
        {
            var claim = User.FindFirstValue(PortfolioUserIdClaim);
            if (int.TryParse(claim, out var parsed) && parsed > 0)
            {
                contextUserId = parsed;
            }
        }
        else if (User.IsInRole("Admin") && userId is not null && userId.Value > 0)
        {
            contextUserId = userId.Value;
        }

        var users = User.IsInRole("Admin") ? await _repo.GetUsersAsync() : new();

        var vm = new DashboardViewModel
        {
            Users = users,
            ContextUserId = contextUserId,
        };

        return View(vm);
    }
}
