using System.ComponentModel.DataAnnotations;

namespace SalesManagement.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Nome da Loja")]
    public string StoreName { get; set; } = "SALESUP";

    [Display(Name = "CNPJ")]
    public string? Cnpj { get; set; }

    [Display(Name = "Endereço")]
    public string? StoreAddress { get; set; }

    [Display(Name = "Telefone")]
    public string? StorePhone { get; set; }

    [Display(Name = "Administrador")]
    public bool IsAdmin { get; set; } = true;
}