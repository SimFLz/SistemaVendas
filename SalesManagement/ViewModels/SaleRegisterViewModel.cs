using System.ComponentModel.DataAnnotations;
using SalesManagement.Models.Enums;

namespace SalesManagement.ViewModels;

public class SaleRegisterViewModel
{
    public List<SaleItemViewModel> Items { get; set; } = new List<SaleItemViewModel>();

    // Mantido para compatibilidade com vendas antigas
    [Display(Name = "Forma de Pagamento")]
    public PaymentMethod PaymentMethod { get; set; }

    [Display(Name = "Parcelas")]
    [Range(1, 12)]
    public int Installments { get; set; } = 1;

    [Display(Name = "Desconto Geral")]
    public decimal GeneralDiscount { get; set; } = 0;

    // 🔧 NOVO: pagamentos múltiplos
    public List<SalePaymentViewModel> Payments { get; set; } = new List<SalePaymentViewModel>();

    public decimal Subtotal => Items.Sum(i => i.UnitPrice * i.Quantity);
    public decimal TotalDiscount => Items.Sum(i => i.Discount) + GeneralDiscount;
    public decimal TotalAmount => Subtotal - TotalDiscount;
    public decimal AmountPaid => Payments.Sum(p => p.Amount);
    public decimal Change => AmountPaid - TotalAmount;
}