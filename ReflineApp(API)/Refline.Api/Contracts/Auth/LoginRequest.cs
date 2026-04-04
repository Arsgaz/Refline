using System.ComponentModel.DataAnnotations;

namespace Refline.Api.Contracts.Auth;

public sealed class LoginRequest
{
    [Required]
    [MaxLength(100)]
    public string Login { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
