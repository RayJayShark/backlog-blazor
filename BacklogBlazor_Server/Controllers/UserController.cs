using BacklogBlazor_Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BacklogBlazor_Server.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class UserController : Controller
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

    private AuthDataService _authDataService;
    private UserDataService _userDataService;
    private ILogger<UserController> _logger;
    private readonly string _frontend;

    public UserController(AuthDataService authDataService, UserDataService userDataService, ILogger<UserController> logger, IConfiguration config)
    {
        _authDataService = authDataService;
        _userDataService = userDataService;
        _logger = logger;
        _frontend = config["Frontend"];
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile avatarFile)
    {
        var userId = await GetUserId();

        if (userId < 0)
            return Forbid();

        var fileExt = Path.GetExtension(avatarFile.FileName).ToLower();
        if (AllowedExtensions.All(ext => ext != fileExt))
            return BadRequest("Must be JPEG, PNG, or GIF");

        var fileName = $"u{userId}_{Path.GetRandomFileName().Replace(".", "")}{fileExt}";
        var filePath = Environment.GetEnvironmentVariable("AVATAR_DIR").TrimEnd('/', '\\') + $"/{fileName}";
        var fileStream = new FileStream(filePath, FileMode.CreateNew);

        try
        {
            await avatarFile.CopyToAsync(fileStream);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error saving avatar: {Message}", ex.Message);
            return StatusCode(500);
        }

        await _userDataService.UpdateUserAvatar(userId, _frontend.TrimEnd('/') + $"/avatars/{fileName}");

        return Ok();
    }

    #region HelperMethods
    
    private async Task<long> GetUserId()
    {
        if (!User.HasClaim(c => c.Type == "userId"))
            return -1;

        var userIdString = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        if (!long.TryParse(userIdString, out var userIdLong))
            return -1;

        if (!await _authDataService.IsValidUser(userIdLong))
            return -1;

        return userIdLong;
    }
    
    #endregion
}
