namespace BacklogBlazor_Shared.Models.Authentication;

public class TokenModel
{
    public string JwtToken { get; set; }
    public string RefreshToken { get; set; }
}