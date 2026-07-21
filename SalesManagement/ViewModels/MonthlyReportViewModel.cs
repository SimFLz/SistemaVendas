using SalesManagement.Models.Enums;

namespace SalesManagement.ViewModels;

public class MonthlyReportViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int TotalSales { get; set; }
    public decimal AverageTicket { get; set; }
    public Dictionary<PaymentMethod, decimal> SalesByPaymentMethod { get; set; } = new();
}