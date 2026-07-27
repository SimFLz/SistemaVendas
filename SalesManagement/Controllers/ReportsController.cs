using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Data;
using SalesManagement.Models;
using SalesManagement.Models.Enums;
using SalesManagement.ViewModels;

namespace SalesManagement.Controllers;
[Authorize]
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

        // INÍCIO do dia selecionado
        var startOfDay = selectedDate.Date;
        // FIM do dia selecionado  
        var endOfDay = startOfDay.AddDays(1);

        // DEBUG: mostrar no console o intervalo
        Console.WriteLine($"=== RELATÓRIO DIÁRIO ===");
        Console.WriteLine($"Data selecionada: {selectedDate}");
        Console.WriteLine($"Start: {startOfDay}");
        Console.WriteLine($"End: {endOfDay}");

        // Buscar TODAS as vendas do dia (não canceladas)
        var sales = await _context.Sales
            .Where(s => s.SaleDate >= startOfDay && s.SaleDate < endOfDay && s.Status == SaleStatus.Completed)
            .ToListAsync();

        Console.WriteLine($"Vendas encontradas: {sales.Count}");

        foreach (var s in sales)
        {
            Console.WriteLine($"  Venda #{s.Id} - {s.SaleDate} - {s.TotalAmount} - {s.Status}");
        }

        var totalRevenue = sales.Sum(s => s.TotalAmount);
        var totalSales = sales.Count;
        var averageTicket = totalSales > 0 ? totalRevenue / totalSales : 0;

        var salesByPayment = sales
            .GroupBy(s => s.PaymentMethod)
            .ToDictionary(g => g.Key, g => g.Sum(s => s.TotalAmount));

        // Buscar caixa aberto do dia atual (para botão fechar caixa)
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var openCashRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(c => c.OpenDate >= today && c.OpenDate < tomorrow && c.Status == CashRegisterStatus.Open);

        var viewModel = new DailyReportViewModel
        {
            Date = selectedDate,
            TotalRevenue = totalRevenue,
            TotalSales = totalSales,
            AverageTicket = averageTicket,
            SalesByPaymentMethod = salesByPayment
        };

        ViewData["SelectedDate"] = selectedDate.ToString("yyyy-MM-dd");
        ViewData["OpenCashRegister"] = openCashRegister;

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
            .Where(s => s.SaleDate >= startDate && s.SaleDate < endDate && s.Status == SaleStatus.Completed)
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