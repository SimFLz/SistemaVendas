using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Models.Enums;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Dashboard";

        // Buscar caixa aberto hoje
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var openRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.OpenDate >= today && c.OpenDate < tomorrow && c.Status == CashRegisterStatus.Open);

        if (openRegister == null)
        {
            return View(new DashboardViewModel());
        }

        // Vendas do caixa atual (DESDE a abertura)
        var todaySales = await _context.Sales
            .Where(s => s.SaleDate >= openRegister.OpenDate && s.Status != SaleStatus.Cancelled)
            .ToListAsync();

        var totalRevenue = todaySales.Sum(s => s.TotalAmount);
        var totalSales = todaySales.Count;
        var averageTicket = totalSales > 0 ? totalRevenue / totalSales : 0;

        var salesByPayment = todaySales
            .GroupBy(s => s.PaymentMethod)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.TotalAmount));

        var recentSales = await _context.Sales
            .Where(s => s.SaleDate >= openRegister.OpenDate && s.Status != SaleStatus.Cancelled)
            .OrderByDescending(s => s.SaleDate)
            .Take(10)
            .Select(s => new SaleSummaryViewModel
            {
                Id = s.Id,
                SaleDate = s.SaleDate,
                TotalAmount = s.TotalAmount,
                PaymentMethod = s.PaymentMethod
            })
            .ToListAsync();

        var viewModel = new DashboardViewModel
        {
            TotalRevenueToday = totalRevenue,
            TotalSalesToday = totalSales,
            AverageTicketToday = averageTicket,
            SalesByPaymentMethod = salesByPayment,
            RecentSales = recentSales
        };

        return View(viewModel);
    }
}