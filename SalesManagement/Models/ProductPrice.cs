using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalesManagement.Models;

public class ProductPrice
{
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "O preço é obrigatório.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O preço deve ser maior que zero.")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Preço")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "O código de barras é obrigatório.")]
    [StringLength(50)]
    [Display(Name = "Código de Barras")]
    public string Barcode { get; set; } = string.Empty;

    // Navegação
    public Product Product { get; set; } = null!;
}