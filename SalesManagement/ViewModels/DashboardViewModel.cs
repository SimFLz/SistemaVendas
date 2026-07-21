using SalesManagement.Models.Enums;

namespace SalesManagement.ViewModels;

public class DashboardViewModel
{
    public decimal TotalRevenueToday { get; set; }
    public int TotalSalesToday { get; set; }
    public decimal AverageTicketToday { get; set; }
    public Dictionary<PaymentMethod, decimal> SalesByPaymentMethod { get; set; } = new();
    public List<SaleSummaryViewModel> RecentSales { get; set; } = new();
}

public class SaleSummaryViewModel
{
    public int Id { get; set; }
    public DateTime SaleDate { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
}