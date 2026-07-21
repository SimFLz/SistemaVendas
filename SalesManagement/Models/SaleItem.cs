using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalesManagement.Models;

public class SaleItem
{
    public int Id { get; set; }

    [Required]
    public int SaleId { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "A quantidade é obrigatória.")]
    [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser pelo menos 1.")]
    [Display(Name = "Quantidade")]
    public int Quantity { get; set; } = 1;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "O preço unitário deve ser maior que zero.")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Preço Unitário")]
    [DataType(DataType.Currency)]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Desconto")]
    [DataType(DataType.Currency)]
    public decimal Discount { get; set; } = 0;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Total")]
    [DataType(DataType.Currency)]
    public decimal TotalPrice { get; set; }

    // Navegação
    public Sale Sale { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
