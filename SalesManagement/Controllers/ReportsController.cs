using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models.Enums;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;

public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return RedirectToAction("Daily");
    }

    public async Task<IActionResult> Daily(DateTime? date)
    {
        ViewData["Title"] = "Relatório Diário";

        var selectedDate = date ?? DateTime.Today;
        var nextDay = selectedDate.AddDays(1);

        var sales = await _context.Sales
            .Where(s => s.SaleDate >= selectedDate && s.SaleDate < nextDay && s.Status != SaleStatus.Cancelled)
            .ToListAsync();

        var totalRevenue = sales.Sum(s => s.TotalAmount);
        var totalSales = sales.Count;
        var averageTicket = totalSales > 0 ? totalRevenue / totalSales : 0;

        var salesByPayment = sales
            .GroupBy(s => s.PaymentMethod)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.TotalAmount));

        var viewModel = new DailyReportViewModel
        {
            Date = selectedDate,
            TotalRevenue = totalRevenue,
            TotalSales = totalSales,
            AverageTicket = averageTicket,
            SalesByPaymentMethod = salesByPayment
        };

        ViewData["SelectedDate"] = selectedDate.ToString("yyyy-MM-dd");
        return View(viewModel);
    }

    public async Task<IActionResult> Monthly(int? year, int? month)
    {
        ViewData["Title"] = "Relatório Mensal";

        var selectedYear = year ?? DateTime.Today.Year;
        var selectedMonth = month ?? DateTime.Today.Month;

        var startDate = new DateTime(selectedYear, selectedMonth, 1);
        var endDate = startDate.AddMonths(1);

        var sales = await _context.Sales
            .Where(s => s.SaleDate >= startDate && s.SaleDate < endDate && s.Status != SaleStatus.Cancelled)
            .ToListAsync();

        var totalRevenue = sales.Sum(s => s.TotalAmount);
        var totalSales = sales.Count;
        var averageTicket = totalSales > 0 ? totalRevenue / totalSales : 0;

        var salesByPayment = sales
            .GroupBy(s => s.PaymentMethod)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.TotalAmount));

        var viewModel = new MonthlyReportViewModel
        {
            Year = selectedYear,
            Month = selectedMonth,
            MonthName = new DateTime(selectedYear, selectedMonth, 1).ToString("MMMM", new System.Globalization.CultureInfo("pt-BR")),
            TotalRevenue = totalRevenue,
            TotalSales = totalSales,
            AverageTicket = averageTicket,
            SalesByPaymentMethod = salesByPayment
        };

        ViewData["SelectedYear"] = selectedYear;
        ViewData["SelectedMonth"] = selectedMonth;
        return View(viewModel);
    }
}