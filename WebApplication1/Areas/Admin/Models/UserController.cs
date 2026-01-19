using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PortfolioWeb.Areas.Admin.Models;
using PortfolioWeb.Data;

namespace PortfolioWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,User")]
public class UsersController : Controller
{
    private readonly PortfolioRepository _repo;
    private readonly ILogger<UsersController> _logger;

    private const string PortfolioUserIdClaim = "PortfolioUserId";

    public UsersController(PortfolioRepository repo, ILogger<UsersController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult MyProfile()
    {
        if (User?.Identity?.IsAuthenticated != true)
        {
            return RedirectToAction("Login", "Account", new { area = "Admin" });
        }

        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        var claim = User.FindFirstValue(PortfolioUserIdClaim);
        if (!int.TryParse(claim, out var userId) || userId <= 0)
        {
            return Forbid();
        }

        return RedirectToAction("Edit", new { id = userId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (id <= 0)
        {
            return NotFound();
        }

        if (!CanEditUser(id))
        {
            return Forbid();
        }

        var user = await _repo.GetUserByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var vm = new UserEditModel
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Phone = user.Phone,
            Location = user.Location,
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditModel input)
    {
        if (!CanEditUser(input.Id))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            var ok = await _repo.UpdateUserAsync(input.Id, input.Username, input.Email, input.Phone, input.Location);
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "Unable to save right now.");
                return View(input);
            }

            if (User.IsInRole("User"))
            {
                var refreshedClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, input.Id.ToString()),
                    new Claim(ClaimTypes.Name, input.Username?.Trim() ?? string.Empty),
                    new Claim(ClaimTypes.Email, input.Email?.Trim() ?? string.Empty),
                    new Claim(ClaimTypes.Role, "User"),
                    new Claim(PortfolioUserIdClaim, input.Id.ToString()),
                };

                var identity = new ClaimsIdentity(refreshedClaims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            }

            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update user {UserId}.", input.Id);
            ModelState.AddModelError(string.Empty, "Unable to save right now.");
            return View(input);
        }
    }

    private bool CanEditUser(int id)
    {
        if (User.IsInRole("Admin"))
        {
            return true;
        }

        if (!User.IsInRole("User"))
        {
            return false;
        }

        var claim = User.FindFirstValue(PortfolioUserIdClaim);
        return int.TryParse(claim, out var userId) && userId > 0 && userId == id;
    }
}
