using SalesManagement.Models.Enums;

namespace SalesManagement.ViewModels;

public class DailyReportViewModel
{
    public DateTime Date { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalSales { get; set; }
    public decimal AverageTicket { get; set; }
    public Dictionary<PaymentMethod, decimal> SalesByPaymentMethod { get; set; } = new();
}