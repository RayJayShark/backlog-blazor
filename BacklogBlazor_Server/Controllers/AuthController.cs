using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BacklogBlazor_Server.Services;
using BacklogBlazor_Shared.Models.Authentication;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using BC = BCrypt.Net.BCrypt;

namespace BacklogBlazor_Server.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : Controller
{
    private readonly AuthDataService _authDataService;
    private readonly string _jwtSecret;

    public AuthController(AuthDataService authDataService)
    {
        _authDataService = authDataService;
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
    }
    
    [HttpPost("login")]
    [Consumes("application/json")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        var passwordHash = await _authDataService.GetHashedPassword(loginRequest.Email);

        if (string.IsNullOrWhiteSpace(passwordHash.Hash))
            return Unauthorized();
        
        var validLogin = BC.EnhancedVerify(loginRequest.Password, passwordHash.Hash, HashType.SHA384);

        if (!validLogin)
            return Unauthorized();

        return Ok(GenerateJwtToken(passwordHash.UserId, passwordHash.Username));
    }

    [HttpPost("register")]
    [Consumes("application/json")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest registerRequest)
    {
        if (await _authDataService.EmailExists(registerRequest.Email))
            return BadRequest("An account for the email provided already exists");

        var hashedPassword = BC.EnhancedHashPassword(registerRequest.Password, HashType.SHA384);

        var userId = await _authDataService.RegisterUser(registerRequest.Email, registerRequest.Username, hashedPassword);

        if (!userId.HasValue)
            return StatusCode(500);
        
        //TODO: Send confirmation email


        return Ok(GenerateJwtToken(userId.Value, registerRequest.Username));
    }

    //TODO: Refresh endpoint
    
    #region HelperMethods

    private string GenerateJwtToken(long userId, string username,  DateTime? createdAt = null)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var currentTime = DateTime.Now.ToString("O");
        var createdAtTime = createdAt?.ToString("O") ?? currentTime;

        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new("username", username),
            new("createdAt", createdAtTime),
            new("updatedAt", currentTime)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            signingCredentials: credentials,
            expires: DateTime.Now + TimeSpan.FromMinutes(30)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion
}