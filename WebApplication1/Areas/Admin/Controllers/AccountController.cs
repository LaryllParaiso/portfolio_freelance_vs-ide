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
public class AccountController : Controller
{
    private readonly PortfolioRepository _repo;
    private readonly ILogger<AccountController> _logger;

    public AccountController(PortfolioRepository repo, ILogger<AccountController> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Entry()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        var adminExists = await _repo.AdminExistsAsync();
        if (!adminExists)
        {
            return RedirectToAction("Signup", "Account", new { area = "Admin" });
        }

        return RedirectToAction("Login", "Account", new { area = "Admin" });
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new AdminLoginInput());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginInput input, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            var admin = await _repo.AuthenticateAdminAsync(input.Email, input.Password);
            if (admin is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(input);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Name, admin.Username),
                new Claim(ClaimTypes.Email, admin.Email),
                new Claim(ClaimTypes.Role, "Admin"),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin login failed.");
            ModelState.AddModelError(string.Empty, "Unable to sign in right now. Please try again.");
            return View(input);
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Signup()
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        var adminExists = await _repo.AdminExistsAsync();
        if (adminExists)
        {
            return RedirectToAction("Login", "Account", new { area = "Admin" });
        }

        return View(new AdminSignupInput());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Signup(AdminSignupInput input)
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        var adminExists = await _repo.AdminExistsAsync();
        if (adminExists)
        {
            return RedirectToAction("Login", "Account", new { area = "Admin" });
        }

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            var created = await _repo.CreateAdminAsync(input.Username, input.Email, input.Phone, input.Location, input.Password);
            if (!created)
            {
                ModelState.AddModelError(string.Empty, "Unable to create admin (an admin account may already exist).");
                return View(input);
            }

            return RedirectToAction("Login", "Account", new { area = "Admin" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin signup failed.");
            ModelState.AddModelError(string.Empty, "Unable to create account right now. Please try again.");
            return View(input);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account", new { area = "Admin" });
    }
}
