using System.ComponentModel.DataAnnotations;

namespace SalesManagement.ViewModels;

public class ProductPriceViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O preço é obrigatório.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O preço deve ser maior que zero.")]
    [Display(Name = "Preço")]
    public decimal Price { get; set; }

    [Display(Name = "Código de Barras")]
    public string Barcode { get; set; } = string.Empty;
}