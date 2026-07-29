using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Models.Enums;

namespace SalesManagement.Controllers;

public class CashRegisterController : BaseController
{
    private readonly ApplicationDbContext _context;

    public CashRegisterController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var userId = GetCurrentUserId();

        var openRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.UserId == userId && c.OpenDate >= today && c.OpenDate < tomorrow && c.Status == CashRegisterStatus.Open);

        if (openRegister != null)
            return RedirectToAction("Index", "Sales");

        ViewData["Title"] = "Abertura de Caixa";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open([Bind("InitialAmount,Observations")] CashRegister cashRegister)
    {
        // 🔧 LIMPA ERROS DE CAMPOS PREENCHIDOS PELO SERVIDOR
        ModelState.Remove("OpenDate");
        ModelState.Remove("Status");
        ModelState.Remove("UserId");
        ModelState.Remove("User");      // navegação
        ModelState.Remove("FinalAmount");
        ModelState.Remove("CloseDate");

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var userId = GetCurrentUserId();

        var existingOpen = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.UserId == userId && c.OpenDate >= today && c.OpenDate < tomorrow && c.Status == CashRegisterStatus.Open);

        if (existingOpen != null)
        {
            ModelState.AddModelError("", "O caixa já está aberto.");
            ViewData["Title"] = "Abertura de Caixa";
            return View("Index", cashRegister);
        }

        if (ModelState.IsValid)
        {
            cashRegister.OpenDate = DateTime.Now;
            cashRegister.Status = CashRegisterStatus.Open;
            cashRegister.UserId = userId;

            _context.CashRegisters.Add(cashRegister);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove($"SaleCart_{userId}");
            TempData["Success"] = "Caixa aberto com sucesso!";
            return RedirectToAction("Receipt", new { id = cashRegister.Id, type = "open" });
        }

        ViewData["Title"] = "Abertura de Caixa";
        return View("Index", cashRegister);
    }

    public async Task<IActionResult> Receipt(int? id, string type)
    {
        if (id == null) return NotFound();

        var cashRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == GetCurrentUserId());

        if (cashRegister == null) return NotFound();

        var store = await _context.Users.FindAsync(GetCurrentUserId());
        ViewData["StoreName"] = store?.StoreName ?? "SALESUP";
        ViewData["StoreCnpj"] = store?.Cnpj;
        ViewData["StoreAddress"] = store?.StoreAddress;
        ViewData["StorePhone"] = store?.StorePhone;

        ViewData["Type"] = type;
        ViewData["Title"] = type == "close" ? "Fechamento de Caixa" : "Abertura de Caixa";

        if (type == "close")
        {
            var startDate = cashRegister.OpenDate.Date;
            var endDate = startDate.AddDays(1);

            var sales = await _context.Sales
     .Where(s => s.UserId == GetCurrentUserId() && s.SaleDate >= startDate && s.SaleDate < endDate && s.Status != SaleStatus.Cancelled)
     .Include(s => s.Items)
     .ThenInclude(i => i.Product)
     .Include(s => s.Payments) // 🔧 ADICIONAR
     .ToListAsync();

            var totalRevenue = sales.Sum(s => s.TotalAmount);
            var totalSales = sales.Count;

            // 🔧 Agrupa pelos pagamentos reais
            var saleIds = sales.Select(s => s.Id).ToList();
            var payments = await _context.SalePayments
                .Where(sp => saleIds.Contains(sp.SaleId))
                .ToListAsync();

            var salesByPayment = payments
                .GroupBy(p => p.PaymentMethod)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

            ViewData["TotalRevenue"] = totalRevenue;
            ViewData["TotalSales"] = totalSales;
            ViewData["SalesByPayment"] = salesByPayment;
            ViewData["Sales"] = sales;
        }

        return View(cashRegister);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id, decimal? finalAmount, string? observations)
    {
        var cashRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == GetCurrentUserId());

        if (cashRegister == null) return NotFound();

        if (cashRegister.Status == CashRegisterStatus.Closed)
        {
            TempData["Error"] = "Este caixa já foi fechado.";
            return RedirectToAction("Daily", "Reports");
        }

        var startDate = cashRegister.OpenDate;
        var endDate = DateTime.Now;

        var sales = await _context.Sales
            .Where(s => s.UserId == GetCurrentUserId() && s.SaleDate >= startDate && s.SaleDate <= endDate && s.Status != SaleStatus.Cancelled)
            .ToListAsync();

        var totalSales = sales.Sum(s => s.TotalAmount);

        cashRegister.CloseDate = DateTime.Now;
        cashRegister.FinalAmount = totalSales + cashRegister.InitialAmount;
        cashRegister.Observations = observations;
        cashRegister.Status = CashRegisterStatus.Closed;

        _context.Update(cashRegister);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Caixa fechado com sucesso!";
        return RedirectToAction("Closed", new { id = cashRegister.Id });
    }

    public async Task<IActionResult> Closed(int? id)
    {
        if (id == null) return NotFound();

        var cashRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == GetCurrentUserId());

        if (cashRegister == null) return NotFound();

        var store = await _context.Users.FindAsync(GetCurrentUserId());
        ViewData["StoreName"] = store?.StoreName ?? "SALESUP";
        ViewData["StoreCnpj"] = store?.Cnpj;
        ViewData["StoreAddress"] = store?.StoreAddress;
        ViewData["StorePhone"] = store?.StorePhone;

        ViewData["Title"] = "Caixa Fechado";

        var sales = await _context.Sales
     .Where(s => s.UserId == GetCurrentUserId() && s.SaleDate >= cashRegister.OpenDate
              && s.SaleDate <= (cashRegister.CloseDate ?? DateTime.Now)
              && s.Status != SaleStatus.Cancelled)
     .Include(s => s.Items)
     .ThenInclude(i => i.Product)
     .Include(s => s.Payments) // 🔧 ADICIONAR
     .ToListAsync();

        var totalRevenue = sales.Sum(s => s.TotalAmount);
        var totalSalesCount = sales.Count;

        var saleIds = sales.Select(s => s.Id).ToList();
        var payments = await _context.SalePayments
            .Where(sp => saleIds.Contains(sp.SaleId))
            .ToListAsync();

        var salesByPayment = payments
            .GroupBy(p => p.PaymentMethod)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        ViewData["TotalRevenue"] = totalRevenue;
        ViewData["TotalSales"] = totalSalesCount;
        ViewData["SalesByPayment"] = salesByPayment;
        ViewData["Sales"] = sales;

        return View(cashRegister);
    }

    public async Task<IActionResult> List()
    {
        ViewData["Title"] = "Histórico de Caixas";

        var cashRegisters = await _context.CashRegisters
            .Where(c => c.UserId == GetCurrentUserId())
            .OrderByDescending(c => c.OpenDate)
            .ToListAsync();

        return View(cashRegisters);
    }
}