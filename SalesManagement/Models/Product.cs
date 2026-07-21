using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalesManagement.Models;

public class Product
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

    [Required(ErrorMessage = "O preço é obrigatório.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "O preço deve ser maior que zero.")]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Preço")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Display(Name = "Ativo")]
    public bool IsActive { get; set; } = true;

    // Navegação
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
