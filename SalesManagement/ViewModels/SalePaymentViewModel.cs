using System.ComponentModel.DataAnnotations;
using SalesManagement.Models.Enums;

namespace SalesManagement.ViewModels;

public class SalePaymentViewModel
{
    public PaymentMethod PaymentMethod { get; set; }

    [Required(ErrorMessage = "Informe o valor.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }

    [Range(1, 12)]
    public int Installments { get; set; } = 1;
}