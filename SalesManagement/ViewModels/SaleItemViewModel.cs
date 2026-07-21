namespace SalesManagement.ViewModels;

public class SaleItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal Discount { get; set; } = 0;
    public decimal TotalPrice => (UnitPrice * Quantity) - Discount;
}