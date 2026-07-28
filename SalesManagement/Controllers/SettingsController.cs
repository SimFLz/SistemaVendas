using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

public class SettingsController : BaseController
{
    private readonly ApplicationDbContext _context;

    public SettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _context.Users.FindAsync(GetCurrentUserId());
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(StoreSettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "Configurações da Loja";
            return View(model);
        }

        var user = await _context.Users.FindAsync(GetCurrentUserId());
        if (user == null) return NotFound();

        if (await _context.Users.AnyAsync(u => u.Email == model.Email && u.Id != user.Id))
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