using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Controllers;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _context;

    public SettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Settings
    public async Task<IActionResult> Index()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
            return RedirectToAction("Login", "Account");

        var user = await _context.Users.FindAsync(userId);
        if (user == null) return NotFound();

        var model = new StoreSettingsViewModel
        {
            Id = user.Id,
            StoreName = user.StoreName,
            Cnpj = user.Cnpj,
            StoreAddress = user.StoreAddress,
            StorePhone = user.StorePhone,
            Email = user.Email
        };

        ViewData["Title"] = "Configurações da Loja";
        return View(model);
    }

    // POST: /Settings
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(StoreSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Configurações da Loja";
            return View(model);
        }

        var user = await _context.Users.FindAsync(model.Id);
        if (user == null) return NotFound();

        // Verificar se o e-mail já existe em outro usuário
        if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != model.Id))
        {
            ModelState.AddModelError("Email", "Este e-mail já está em uso.");
            ViewData["Title"] = "Configurações da Loja";
            return View(model);
        }

        user.StoreName = model.StoreName;
        user.Cnpj = model.Cnpj;
        user.StoreAddress = model.StoreAddress;
        user.StorePhone = model.StorePhone;
        user.Email = model.Email;

        // Atualizar senha se informada
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            user.PasswordHash = AccountController.HashPassword(model.NewPassword);
        }

        _context.Update(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Dados da loja atualizados com sucesso!";
        return RedirectToAction(nameof(Index));
    }
}