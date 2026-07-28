using System.ComponentModel.DataAnnotations;

namespace SalesManagement.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "O nome da loja é obrigatório.")]
    [Display(Name = "Nome da Loja")]
    public string StoreName { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "A senha deve ter no mínimo 6 caracteres.")]
    [Display(Name = "Senha")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a senha.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "As senhas não conferem.")]
    [Display(Name = "Confirmar Senha")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CNPJ é obrigatório.")]
    [Display(Name = "CNPJ")]
    public string Cnpj { get; set; } = string.Empty;

    [Required(ErrorMessage = "O endereço é obrigatório.")]
    [Display(Name = "Endereço da Loja")]
    public string StoreAddress { get; set; } = string.Empty;

    [Required(ErrorMessage = "O telefone é obrigatório.")]
    [Display(Name = "Telefone")]
    public string StorePhone { get; set; } = string.Empty;
}