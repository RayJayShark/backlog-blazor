using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BacklogBlazor_Shared.Models;
using BacklogBlazor_Shared.Models.Authentication;
using BacklogBlazor.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace BacklogBlazor.Services;

public class AuthorizedApiService
{
    public User User { get; set; }
    private bool _rememberUser = false;

    private readonly HttpClient _httpClient;
    private readonly JwtSecurityTokenHandler _jwtTokenHandler;
    private readonly SessionService _sessionService;
    private readonly LocalService _localService;

    public AuthorizedApiService(HttpClient httpClient, SessionService sessionService, LocalService localService)
    {
        _httpClient = httpClient;
        _jwtTokenHandler = new JwtSecurityTokenHandler();
        _sessionService = sessionService;
        _localService = localService;
        User = new User();
    }

    public async Task InitializeAsync()
    {
        var tokenModel = new TokenModel
        {
            JwtToken = await _sessionService.GetJwtTokenAsync(),
            RefreshToken = await _sessionService.GetRefreshTokenAsync()
        };

        if (string.IsNullOrWhiteSpace(tokenModel.RefreshToken))
            tokenModel.RefreshToken = await _localService.GetRefreshTokenAsync();

        if (!string.IsNullOrWhiteSpace(tokenModel.RefreshToken))
            _rememberUser = true;
            
        // If token is invalid or expired, and refresh token exists, attempt refresh
        if ((!SetBearerToken(tokenModel) || !User.IsAuthenticated)
            && !string.IsNullOrWhiteSpace(tokenModel.RefreshToken))
        {
            // No supported way to await this. Cross your fingers for no race condition!
            try
            {
                await RefreshJwtToken();
            }
            catch (HttpRequestException ex)
            {
                // Remove token to take it to take user to login page
                User.AuthToken = new JwtSecurityToken();
            }
        }

        if (!User.IsAuthenticated)
        {
            _sessionService.RemoveJwtToken();
            _sessionService.RemoveRefreshToken();
            _localService.RemoveRefreshToken();
            _rememberUser = false;
        }
    }

    public bool SetBearerToken(TokenModel tokenModel, bool? rememberUser = null)
    {
        if (rememberUser.HasValue)
            _rememberUser = rememberUser.Value;
            
        try
        {
            User.RefreshToken = _jwtTokenHandler.ReadJwtToken(tokenModel.RefreshToken);
            User.RefreshTokenString = tokenModel.RefreshToken;
            _sessionService.SetRefreshToken(tokenModel.RefreshToken);
            if (_rememberUser)
                _localService.SetRefreshToken(tokenModel.RefreshToken);

            User.AuthToken = _jwtTokenHandler.ReadJwtToken(tokenModel.JwtToken);
            User.AuthTokenString = tokenModel.JwtToken;
            _sessionService.SetJwtToken(tokenModel.JwtToken);
            
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

    public async Task Logout()
    {
        User.AuthToken = null;
        User.AuthTokenString = string.Empty;
        User.RefreshToken = null;
        User.RefreshTokenString = string.Empty;
        _sessionService.RemoveJwtToken();
        _sessionService.RemoveRefreshToken();
        _localService.RemoveRefreshToken();
    }

    public bool RequireAuthentication(NavigationManager nav)
    {
        if (!User.IsAuthenticated)
        {
            nav.NavigateTo("login");
            return false;
        }

        return true;
    }
    
    private async Task<T?> SendRequest<T>(HttpMethod method, string endpoint, string token, object? payload = null, bool isRefresh = false)
    {
        return (T?) await SendRequest(method, endpoint, token, payload, isRefresh, typeof(T));
    }

    private async Task<object?> SendRequest(HttpMethod method, string endpoint, string token, object? payload = null,
        bool isRefresh = false, Type? type = null)
    {
        // Skip this check is refreshing to avoid recursion loop
        if (!isRefresh && !User.IsAuthTokenValid)
        {
            var refreshSuccess = await RefreshJwtToken();
            
            if (!refreshSuccess || !User.IsAuthenticated)
                throw new Exception("Unable to authenticate");
        }
        
        var request = new HttpRequestMessage
        {
            Method = method,
            RequestUri = new Uri(endpoint, UriKind.Relative)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (payload is not null)
        {
            if (payload is IBrowserFile file)
            {
                var content = new MultipartFormDataContent();
                content.Add(new StreamContent(file.OpenReadStream()), "\"avatarFile\"", file.Name);
                content.First().Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                request.Content = content;
            }
            else
            {
                request.Content = JsonContent.Create(payload);
            }
        }

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("Unsuccessful response code", null, response.StatusCode);
        
        if (type is not null)
            return await response.Content.ReadFromJsonAsync(type);

        return null;
    }

    public async Task<bool> RefreshJwtToken()
    {
        if (!User.IsRefreshTokenValid)
            return false;
        
        // Do not catch exceptions, caught upstream to give user useful notifications
        var tokenModel = await SendRequest<TokenModel>(HttpMethod.Post, "auth/refresh", User.RefreshTokenString, isRefresh: true);

        return SetBearerToken(tokenModel);
    }

    private async Task<long> GetNextBacklogId()
    {
        return await SendRequest<long>(HttpMethod.Get, "backlog/next", User.AuthTokenString);
    }

    public async Task CreateNewBacklog(NavigationManager nav, NotificationService notificationService)
    {
        // Ignore if not logged in
        if (!User.IsAuthenticated)
            return;

        // Call to get new id
        var newId = 0L;
        try
        {
            newId = await GetNextBacklogId();
        }
        catch (HttpRequestException ex)
        {
            switch (ex.StatusCode)
            {
                case HttpStatusCode.InternalServerError:
                    await notificationService.DisplayNotification("Error creating new backlog: Server error", NotificationLevel.Error);
                    break;
                case HttpStatusCode.Forbidden:
                    await notificationService.DisplayNotification("Error creating new backlog: Invalid user. Try logging in!", NotificationLevel.Error);
                    break;
                default:
                    await notificationService.DisplayNotification("Error creating new backlog: Unable to connect to server", NotificationLevel.Error);
                    break;
            }

            return;
        }
        catch (Exception ex)
        {
            await notificationService.DisplayNotification("Error creating new backlog: Unknown error", NotificationLevel.Error);
            return;
        }

        nav.NavigateTo($"backlog/{newId}?newBacklog=true&edit=true");
    }
    
    public async Task<BacklogModel> GetBacklog(long backlogId)
    {
        return await SendRequest<BacklogModel>(HttpMethod.Get, $"backlog/{backlogId}", User.AuthTokenString);
    }

    public async Task<List<BacklogModel>> GetUserBacklogs()
    {
        return await SendRequest<List<BacklogModel>>(HttpMethod.Get, "backlog/list", User.AuthTokenString);
    }

    public async Task<List<RecentBacklog>> GetUserRecentBacklogs()
    {
        return await SendRequest<List<RecentBacklog>>(HttpMethod.Get, "backlog/recent", User.AuthTokenString);
    }

    public async Task SaveBacklog(BacklogModel backlogModel)
    {
        await SendRequest(HttpMethod.Post, "backlog", User.AuthTokenString, backlogModel);
    }

    public async Task DeleteBacklog(long backlogId)
    {
        await SendRequest(HttpMethod.Delete, $"backlog/{backlogId}", User.AuthTokenString);
    }

    public async Task UpdateAvatar(IBrowserFile file)
    {
        await SendRequest(HttpMethod.Post, "user/avatar", User.AuthTokenString, file);
    }
}