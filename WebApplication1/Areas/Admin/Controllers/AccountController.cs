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

    private const string PortfolioUserIdClaim = "PortfolioUserId";

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
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            if (User.IsInRole("User"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home", new { area = "" });
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
            if (admin is not null)
            {
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

            var user = await _repo.AuthenticateUserAsync(input.Email, input.Password);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(input);
            }

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, "User"),
                new Claim(PortfolioUserIdClaim, user.Id.ToString()),
            };

            var userIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var userPrincipal = new ClaimsPrincipal(userIdentity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, userPrincipal);

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
            if (User.IsInRole("Admin"))
            {
                var adminExistsForAdmin = await _repo.AdminExistsAsync();
                ViewData["SignupMode"] = adminExistsForAdmin ? "User" : "Admin";
                ViewData["Title"] = adminExistsForAdmin ? "Create Account" : "Create Admin";
                return View(new AdminSignupInput());
            }

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        var adminExists = await _repo.AdminExistsAsync();
        ViewData["SignupMode"] = adminExists ? "User" : "Admin";
        ViewData["Title"] = adminExists ? "Create Account" : "Create Admin";
        return View(new AdminSignupInput());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Signup(AdminSignupInput input)
    {
        if (User?.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin"))
            {
                var adminExistsForAdmin = await _repo.AdminExistsAsync();
                ViewData["SignupMode"] = adminExistsForAdmin ? "User" : "Admin";
                ViewData["Title"] = adminExistsForAdmin ? "Create Account" : "Create Admin";

                if (!ModelState.IsValid)
                {
                    return View(input);
                }

                try
                {
                    if (!adminExistsForAdmin)
                    {
                        var createdAdmin = await _repo.CreateAdminAsync(input.Username, input.Email, input.Phone, input.Location, input.Password);
                        if (!createdAdmin)
                        {
                            ModelState.AddModelError(string.Empty, "Unable to create admin (an admin account may already exist).");
                            return View(input);
                        }

                        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    }

                    var createdUser = await _repo.CreateUserAsync(input.Username, input.Email, input.Phone, input.Location, input.Password);
                    if (!createdUser)
                    {
                        ModelState.AddModelError(string.Empty, "Unable to create account right now.");
                        return View(input);
                    }

                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "User signup (admin-created) failed.");
                    ModelState.AddModelError(string.Empty, "Unable to create account right now. Please try again.");
                    return View(input);
                }
            }

            return RedirectToAction("Index", "Home", new { area = "" });
        }

        var adminExists = await _repo.AdminExistsAsync();
        ViewData["SignupMode"] = adminExists ? "User" : "Admin";
        ViewData["Title"] = adminExists ? "Create Account" : "Create Admin";

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        try
        {
            if (!adminExists)
            {
                var createdAdmin = await _repo.CreateAdminAsync(input.Username, input.Email, input.Phone, input.Location, input.Password);
                if (!createdAdmin)
                {
                    ModelState.AddModelError(string.Empty, "Unable to create admin (an admin account may already exist).");
                    return View(input);
                }

                return RedirectToAction("Login", "Account", new { area = "Admin" });
            }

            var createdUser = await _repo.CreateUserAsync(input.Username, input.Email, input.Phone, input.Location, input.Password);
            if (!createdUser)
            {
                ModelState.AddModelError(string.Empty, "Unable to create account right now.");
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

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account", new { area = "Admin" });
    }
}
