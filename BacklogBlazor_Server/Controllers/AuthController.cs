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
    private readonly UserDataService _userDataService;
    private readonly ThirdPartyService _thirdPartyService;
    private readonly ILogger<AuthController> _logger;
    private readonly string _jwtSecret;
    private readonly string _refreshSecret;

    public AuthController(AuthDataService authDataService, UserDataService userDataService, ThirdPartyService thirdPartyService, ILogger<AuthController> logger)
    {
        _authDataService = authDataService;
        _userDataService = userDataService;
        _thirdPartyService = thirdPartyService;
        _logger = logger;
        _jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
        _refreshSecret = Environment.GetEnvironmentVariable("REFRESH_SECRET");
    }
    
    [HttpPost("login")]
    [Consumes("application/json")]
    public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
    {
        var passwordHash = await _authDataService.GetHashedPassword(loginRequest.Email);

        if (passwordHash is null || string.IsNullOrWhiteSpace(passwordHash.Hash))
            return Unauthorized();
        
        var validLogin = BC.EnhancedVerify(loginRequest.Password, passwordHash.Hash, HashType.SHA384);

        if (!validLogin)
            return Unauthorized();

        var isDiscordUser = await _userDataService.IsDiscordUser(passwordHash.UserId);

        var avatarUrl = await _userDataService.GetUserAvatarUrl(passwordHash.UserId);

        var tokenModel = new TokenModel
        {
            JwtToken = GenerateJwtToken(passwordHash.UserId, passwordHash.Username, isDiscordUser, avatarUrl),
            RefreshToken = GenerateRefreshToken(passwordHash.UserId, passwordHash.Username, isDiscordUser, avatarUrl)
        };

        return Ok(tokenModel);
    }

    [HttpPost("login/discord")]
    public async Task<IActionResult> DiscordLogin([FromQuery] string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning("Empty code sent to Discord auth");
            return BadRequest();
        }

        var discordAccessToken = await _thirdPartyService.AuthenticateDiscordCode(code);

        if (string.IsNullOrWhiteSpace(discordAccessToken))
        {
            _logger.LogWarning("Problem retrieving Discord access token");
            return BadRequest();
        }

        var discordUser = await _thirdPartyService.GetDiscordUserData(discordAccessToken);

        if (discordUser is null)
        {
            _logger.LogWarning("Unable to get user data from Discord");
            return BadRequest();
        }

        var user = await _authDataService.UpsertDiscordUser(discordUser);

        var tokenModel = new TokenModel
        {
            JwtToken = GenerateJwtToken(user.UserId, user.Username, true, user.AvatarUrl),
            RefreshToken = GenerateRefreshToken(user.UserId, user.Username, true, user.AvatarUrl)
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
            JwtToken = GenerateJwtToken(userId.Value, registerRequest.Username, false),
            RefreshToken = GenerateRefreshToken(userId.Value, registerRequest.Username, false)
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
        
        var isDiscordUserString = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        if (!bool.TryParse(isDiscordUserString, out var isDiscordUserBool))
        {
            isDiscordUserBool = await _userDataService.IsDiscordUser(userIdLong);
        }

        var avatarUrl = await _userDataService.GetUserAvatarUrl(userIdLong);

        var createdAtString = User.Claims.FirstOrDefault(c => c.Type == "createdAt")?.Value;
        if (!DateTime.TryParse(createdAtString, out var createdAtDate))
            return StatusCode(500, "Error parsing createdAt DateTime");

        var tokenModel = new TokenModel
        {
            JwtToken = GenerateJwtToken(userIdLong, username, isDiscordUserBool, avatarUrl),
            RefreshToken = GenerateRefreshToken(userIdLong, username, isDiscordUserBool, avatarUrl, createdAtDate.ToLocalTime())
        };

        return Ok(tokenModel);
    }
    
    #region HelperMethods

    private string GenerateJwtToken(long userId, string username, bool isDiscordUser, string? avatarUrl = "",  DateTime? createdAt = null)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var currentTime = DateTime.Now.ToString("O");
        var createdAtTime = createdAt?.ToString("O") ?? currentTime;

        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new("username", username),
            new("isDiscordUser", isDiscordUser.ToString()),
            new("avatarUrl", avatarUrl ?? string.Empty),
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
    
    private string GenerateRefreshToken(long userId, string username, bool isDiscordUser, string? avatarUrl = "", DateTime? createdAt = null)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_refreshSecret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        var currentTime = DateTime.Now.ToString("O");
        var createdAtTime = createdAt?.ToString("O") ?? currentTime;

        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new("username", username),
            new("isDiscordUser", isDiscordUser.ToString()),
            new("avatarUrl", avatarUrl ?? string.Empty),
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