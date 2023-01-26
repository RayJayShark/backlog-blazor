using System.IdentityModel.Tokens.Jwt;
using BacklogBlazor.Models;

namespace BacklogBlazor.Services;

public class AuthorizedApiService
{
    public User User { get; set; }
    
    private readonly HttpClient _httpClient;
    private readonly JwtSecurityTokenHandler _jwtTokenHandler;

    public AuthorizedApiService(HttpClient httpClient, SessionService sessionService, LocalService localService)
    {
        _httpClient = httpClient;
        _jwtTokenHandler = new JwtSecurityTokenHandler();
        User = new User();

        var jwtToken = sessionService.GetJwtToken();
        if (string.IsNullOrWhiteSpace(jwtToken))
        {
            jwtToken = localService.GetJwtToken();
        }
        
        if (!string.IsNullOrWhiteSpace(jwtToken))
        {
            SetBearerToken(jwtToken);
        }

        if (!User.IsAuthenticated)
        {
            sessionService.RemoveJwtToken();
            localService.RemoveJwtToken();
        }
    }

    public bool SetBearerToken(string authToken)
    {
        try
        {
            User.AuthToken = _jwtTokenHandler.ReadJwtToken(authToken);
            return true;
        }
        catch (ArgumentException ex)
        {
            // Invalid token
            return false;
        }
        catch (Exception ex)
        {
            // Unknown error
            return false;
        }
    }
}