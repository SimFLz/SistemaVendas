using System.ComponentModel.DataAnnotations;

namespace SalesManagement.ViewModels;

public class ProductViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome do produto é obrigatório.")]
    [StringLength(100, ErrorMessage = "O nome deve ter no máximo 100 caracteres.")]
    [Display(Name = "Nome")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "O código do produto é obrigatório.")]
    [StringLength(20, ErrorMessage = "O código deve ter no máximo 20 caracteres.")]
    [Display(Name = "Código")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Ativo")]
    public bool IsActive { get; set; } = true;

    public List<ProductPriceViewModel> Prices { get; set; } = new List<ProductPriceViewModel>();
}