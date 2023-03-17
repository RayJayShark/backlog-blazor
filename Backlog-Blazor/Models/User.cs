using System.IdentityModel.Tokens.Jwt;

namespace BacklogBlazor.Models;

public class User
{
    public string? Username => AuthToken?.Claims.FirstOrDefault(c => c.Type == "username")?.Value 
                               ?? RefreshToken?.Claims.FirstOrDefault(c => c.Type == "username")?.Value;
    public bool IsAuthenticated => IsAuthTokenValid
                                   || IsRefreshTokenValid;
    
    public JwtSecurityToken? AuthToken;

    public bool IsAuthTokenValid => AuthToken is not null
                                    && !string.IsNullOrWhiteSpace(AuthTokenString)
                                    && AuthExpiration > DateTime.Now;
    
    private DateTime AuthExpiration => AuthToken.ValidTo.ToLocalTime();
    public string AuthTokenString { get; set; }

    public JwtSecurityToken? RefreshToken;

    public bool IsRefreshTokenValid => RefreshToken is not null
                                       && !string.IsNullOrWhiteSpace(RefreshTokenString)
                                       && RefreshExpiration > DateTime.Now;
    private DateTime RefreshExpiration => RefreshToken.ValidTo.ToLocalTime();
    public string RefreshTokenString { get; set; }
}