using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace race_day.DTOs;

public class RegisterRequestDto
{
    [Required]
    [DefaultValue("")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [DefaultValue("")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [DefaultValue("")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DefaultValue("")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DefaultValue("")]
    public string Role { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = true)]
    [DefaultValue("")]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    [Required]
    [DefaultValue("")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DefaultValue("")]
    public string Password { get; set; } = string.Empty;
}