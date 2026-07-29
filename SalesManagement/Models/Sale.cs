using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SalesManagement.Models.Enums;

namespace SalesManagement.Models;

public class Sale
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Data da Venda")]
    [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime SaleDate { get; set; } = DateTime.Now;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Valor Total")]
    [DataType(DataType.Currency)]
    public decimal TotalAmount { get; set; }

    [Required(ErrorMessage = "A forma de pagamento é obrigatória.")]
    [Display(Name = "Forma de Pagamento")]
    public PaymentMethod PaymentMethod { get; set; }

    [Display(Name = "Parcelas")]
    [Range(1, 12)]
    public int Installments { get; set; } = 1;

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Desconto")]
    [DataType(DataType.Currency)]
    public decimal Discount { get; set; } = 0;

    [Display(Name = "Status")]
    public SaleStatus Status { get; set; } = SaleStatus.Completed;

    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<SalePayment> Payments { get; set; } = new List<SalePayment>();
    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}