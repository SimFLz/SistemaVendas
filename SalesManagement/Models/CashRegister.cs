using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalesManagement.Models;

public class CashRegister
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Data de Abertura")]
    public DateTime OpenDate { get; set; }

    [Display(Name = "Data de Fechamento")]
    public DateTime? CloseDate { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Valor Inicial")]
    [DataType(DataType.Currency)]
    public decimal InitialAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Display(Name = "Valor Final")]
    [DataType(DataType.Currency)]
    public decimal? FinalAmount { get; set; }

    [Display(Name = "Status")]
    public CashRegisterStatus Status { get; set; } = CashRegisterStatus.Open;

    [StringLength(500)]
    [Display(Name = "Observações")]
    public string? Observations { get; set; }
}

public enum CashRegisterStatus
{
    Open = 0,
    Closed = 1
}