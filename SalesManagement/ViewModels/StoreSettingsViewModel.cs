using System.ComponentModel.DataAnnotations;

namespace SalesManagement.ViewModels;

public class StoreSettingsViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O nome da loja é obrigatório.")]
    [Display(Name = "Nome da Loja")]
    public string StoreName { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CNPJ é obrigatório.")]
    [Display(Name = "CNPJ")]
    public string Cnpj { get; set; } = string.Empty;

    [Required(ErrorMessage = "O endereço é obrigatório.")]
    [Display(Name = "Endereço da Loja")]
    public string StoreAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório.")]
    [Display(Name = "Telefone")]
    public string StorePhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [Display(Name = "E-mail de Acesso")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Nova Senha")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
    public string? NewPassword { get; set; }

    [Display(Name = "Confirmar Nova Senha")]
    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "As senhas não conferem.")]
    public string? ConfirmPassword { get; set; }
}