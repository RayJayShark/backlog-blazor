using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BacklogBlazor_Server.Services;
using BacklogBlazor_Shared.Models.Authentication;
using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using BC = BCrypt.Net.BCrypt;

namespace BacklogBlazor_Server.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : Controller
{
    private readonly AuthDataService _authDataService;
    private readonly string _jwtSecret;
    private readonly string _refreshSecret;

    public AuthController(AuthDataService authDataService)
    {
        _authDataService = authDataService;
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
        _refreshSecret = Environment.GetEnvironmentVariable("REFRESH_SECRET");
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

        var tokenModel = new TokenModel
        {
            JwtToken = GenerateJwtToken(passwordHash.UserId, passwordHash.Username),
            RefreshToken = GenerateRefreshToken(passwordHash.UserId, passwordHash.Username)
        };

        return Ok(tokenModel);
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

        var tokenModel = new TokenModel
        {
            JwtToken = GenerateJwtToken(userId.Value, registerRequest.Username),
            RefreshToken = GenerateRefreshToken(userId.Value, registerRequest.Username)
        };

        return Ok(tokenModel);
    }

    [HttpPost("refresh")]
    [Authorize("Refresh")]
    public async Task<IActionResult> RefreshJwtToken()
    {
        if (!User.HasClaim(c => c.Type == "userId"))
            return Forbid();
        
        var userIdString = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        if (!long.TryParse(userIdString, out var userIdLong))
            return Forbid();

        var username = await _authDataService.GetUsername(userIdLong);

        if (string.IsNullOrWhiteSpace(username))
            return Forbid();

        var createdAtString = User.Claims.FirstOrDefault(c => c.Type == "createdAt")?.Value;
        if (!DateTime.TryParse(createdAtString, out var createdAtDate))
            return StatusCode(500, "Error parsing createdAt DateTime");

        var tokenModel = new TokenModel
        {
            JwtToken = GenerateJwtToken(userIdLong, username),
            RefreshToken = GenerateRefreshToken(userIdLong, username, createdAtDate.ToLocalTime())
        };

        return Ok(tokenModel);
    }
    
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
    
    private string GenerateRefreshToken(long userId, string username, DateTime? createdAt = null)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_refreshSecret));
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
            expires: DateTime.Now + TimeSpan.FromDays(30)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion
}