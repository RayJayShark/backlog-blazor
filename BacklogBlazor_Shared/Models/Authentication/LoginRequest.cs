using System.ComponentModel.DataAnnotations;

namespace BacklogBlazor_Shared.Models.Authentication;

public class LoginRequest
{
    [Required]
    [RegularExpression(Constants.EmailRegex, ErrorMessage = "Please enter a valid email")]
    public string Email { get; set; }
    
    [Required]
    public string Password { get; set; }
}