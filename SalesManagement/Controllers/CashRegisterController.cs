using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Models.Enums;

namespace SalesManagement.Controllers;

[Authorize]
public class CashRegisterController : Controller
{
    private readonly ApplicationDbContext _context;

    public CashRegisterController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /CashRegister (Tela de abertura de caixa)
    public async Task<IActionResult> Index()
    {
        // Verificar se já existe caixa aberto hoje
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var openRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.OpenDate >= today && c.OpenDate < tomorrow && c.Status == CashRegisterStatus.Open);

        if (openRegister != null)
        {
            // Caixa já aberto, redireciona para tela inicial
            return RedirectToAction("Index", "Sales");
        }

        ViewData["Title"] = "Abertura de Caixa";
        return View();
    }

    // POST: /CashRegister/Open
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open([Bind("InitialAmount,Observations")] CashRegister cashRegister)
    {
        // Verificar se já existe caixa aberto hoje
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var existingOpen = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.OpenDate >= today && c.OpenDate < tomorrow && c.Status == CashRegisterStatus.Open);

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

            _context.CashRegisters.Add(cashRegister);
            await _context.SaveChangesAsync();

            // LIMPAR CARRINHO DE VENDAS ANTERIOR
            HttpContext.Session.Remove("SaleCart");

            TempData["Success"] = "Caixa aberto com sucesso!";
            return RedirectToAction("Receipt", new { id = cashRegister.Id, type = "open" });
        }

        ViewData["Title"] = "Abertura de Caixa";
        return View("Index", cashRegister);
    }

    // GET: /CashRegister/Receipt/5?type=open
    // GET: /CashRegister/Receipt/5?type=open
    public async Task<IActionResult> Receipt(int? id, string type)
    {
        if (id == null) return NotFound();

        var cashRegister = await _context.CashRegisters.FindAsync(id);
        if (cashRegister == null) return NotFound();

        // 🔧 DADOS DA LOJA NA NOTINHA
        var store = await _context.Users.FirstOrDefaultAsync();
        ViewData["StoreName"] = store?.StoreName ?? "SALESUP";
        ViewData["StoreCnpj"] = store?.Cnpj;
        ViewData["StoreAddress"] = store?.StoreAddress;
        ViewData["StorePhone"] = store?.StorePhone;

        ViewData["Type"] = type;
        ViewData["Title"] = type == "close" ? "Fechamento de Caixa" : "Abertura de Caixa";

        if (type == "close")
        {
            var today = cashRegister.OpenDate.Date;
            var tomorrow = today.AddDays(1);

            var sales = await _context.Sales
                .Where(s => s.SaleDate >= today && s.SaleDate < tomorrow && s.Status != SaleStatus.Cancelled)
                .Include(s => s.Items)
                .ThenInclude(i => i.Product)
                .ToListAsync();

            var totalRevenue = sales.Sum(s => s.TotalAmount);
            var totalSales = sales.Count;

            var salesByPayment = sales
                .GroupBy(s => s.PaymentMethod)
                .ToDictionary(g => g.Key, g => g.Sum(s => s.TotalAmount));

            ViewData["TotalRevenue"] = totalRevenue;
            ViewData["TotalSales"] = totalSales;
            ViewData["SalesByPayment"] = salesByPayment;
            ViewData["Sales"] = sales;
        }

        return View(cashRegister);
    }

    // POST: /CashRegister/Close
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id, decimal? finalAmount, string? observations)
    {
        var cashRegister = await _context.CashRegisters.FindAsync(id);
        if (cashRegister == null) return NotFound();

        if (cashRegister.Status == CashRegisterStatus.Closed)
        {
            TempData["Error"] = "Este caixa já foi fechado.";
            return RedirectToAction("Daily", "Reports");
        }

        // Buscar total de vendas do dia (DESDE A ABERTURA DO CAIXA, não do dia inteiro)
        var startDate = cashRegister.OpenDate;
        var endDate = DateTime.Now;

        var sales = await _context.Sales
            .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate && s.Status != SaleStatus.Cancelled)
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

    // GET: /CashRegister/Closed/5
    // GET: /CashRegister/Closed/5
    public async Task<IActionResult> Closed(int? id)
    {
        if (id == null) return NotFound();

        var cashRegister = await _context.CashRegisters.FindAsync(id);
        if (cashRegister == null) return NotFound();

        // 🔧 DADOS DA LOJA NA NOTINHA
        var store = await _context.Users.FirstOrDefaultAsync();
        ViewData["StoreName"] = store?.StoreName ?? "SALESUP";
        ViewData["StoreCnpj"] = store?.Cnpj;
        ViewData["StoreAddress"] = store?.StoreAddress;
        ViewData["StorePhone"] = store?.StorePhone;

        ViewData["Title"] = "Caixa Fechado";

        var sales = await _context.Sales
            .Where(s => s.SaleDate >= cashRegister.OpenDate
                     && s.SaleDate <= (cashRegister.CloseDate ?? DateTime.Now)
                     && s.Status != SaleStatus.Cancelled)
            .Include(s => s.Items)
            .ThenInclude(i => i.Product)
            .ToListAsync();

        var totalRevenue = sales.Sum(s => s.TotalAmount);
        var totalSalesCount = sales.Count;

        var salesByPayment = sales
            .GroupBy(s => s.PaymentMethod)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.TotalAmount));

        ViewData["TotalRevenue"] = totalRevenue;
        ViewData["TotalSales"] = totalSalesCount;
        ViewData["SalesByPayment"] = salesByPayment;
        ViewData["Sales"] = sales;

        return View(cashRegister);
    }

    // GET: /CashRegister/List
    public async Task<IActionResult> List()
    {
        ViewData["Title"] = "Histórico de Caixas";

        var cashRegisters = await _context.CashRegisters
            .OrderByDescending(c => c.OpenDate)
            .ToListAsync();

        return View(cashRegisters);
    }
}