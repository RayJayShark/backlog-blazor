using System.IdentityModel.Tokens.Jwt;

namespace BacklogBlazor.Models;

public class User
{
    public string Username => AuthToken.Claims.FirstOrDefault(c => c.Type == "username").Value;
    public bool IsAuthenticated => AuthToken is not null
                                   && !string.IsNullOrWhiteSpace(AuthToken.ToString())
                                   && AuthExpiration > DateTime.Now;
    
    public JwtSecurityToken? AuthToken;
    
    private DateTime AuthExpiration => AuthToken.ValidTo;
}