using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
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

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        // Vendas de hoje (não canceladas)
        var todaySales = await _context.Sales
            .Where(s => s.SaleDate >= today && s.SaleDate < tomorrow && s.Status != SaleStatus.Cancelled)
            .ToListAsync();

        var totalRevenue = todaySales.Sum(s => s.TotalAmount);
        var totalSales = todaySales.Count;
        var averageTicket = totalSales > 0 ? totalRevenue / totalSales : 0;

        // Vendas por forma de pagamento
        var salesByPayment = todaySales
            .GroupBy(s => s.PaymentMethod)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.TotalAmount));

        // Últimas 10 vendas
        var recentSales = await _context.Sales
            .Where(s => s.Status != SaleStatus.Cancelled)
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