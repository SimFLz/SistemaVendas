using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SalesManagement.Models.Enums;

namespace SalesManagement.Models;

public class SalePayment
{
    public int Id { get; set; }

    [Required]
    public int SaleId { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }

    [Range(1, 12)]
    public int Installments { get; set; } = 1;

    public Sale Sale { get; set; } = null!;
}