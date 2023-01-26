using System.ComponentModel.DataAnnotations;

namespace BacklogBlazor_Shared.Models.Authentication;

public class RegisterRequest
{
    [Required]
    [RegularExpression(Constants.EmailRegex, ErrorMessage = "Please enter a valid email")]
    public string Email { get; set; }
    
    [Required]
    public string Username { get; set; }
    
    [Required] 
    [StringLength(56, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 56 characters")]
    public string Password { get; set; }
    
    [Compare("Password", ErrorMessage = "Both passwords must match")]
    public string PasswordConfirm { get; set; }
}