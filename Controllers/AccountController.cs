using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftHandover.Data;
using ShiftHandover.Helpers;
using ShiftHandover.Models.Enums;
using ShiftHandover.Models.ViewModels;
using ShiftHandover.Services;

namespace ShiftHandover.Controllers;

public class AccountController : Controller
{
    private readonly ShiftHandoverContext _context;

    public AccountController(ShiftHandoverContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        model.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var supervisor = await _context.Supervisors
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Email == model.Email);

        // Same message for "unknown e-mail" and "wrong password" so the form
        // cannot be used to enumerate valid supervisor accounts.
        if (supervisor is null || !supervisor.IsActive ||
            !PasswordHasher.Verify(model.Password, supervisor.PasswordHash, supervisor.PasswordSalt))
        {
            ModelState.AddModelError(string.Empty, "Invalid e-mail address or password.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, supervisor.Id.ToString()),
            new(ClaimTypes.Name, supervisor.Email),
            new(ClaimTypes.Email, supervisor.Email),
            new(ClaimTypes.Role, supervisor.Role.ToString()),
            new(ClaimsPrincipalExtensions.SupervisorIdClaimType, supervisor.Id.ToString()),
            new(ClaimsPrincipalExtensions.FullNameClaimType, supervisor.FullName),
            new(ClaimsPrincipalExtensions.EmployeeCodeClaimType, supervisor.EmployeeCode)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

        TempData["Success"] = $"Welcome back, {supervisor.FullName}.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}