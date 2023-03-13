using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BacklogBlazor_Shared.Models;
using BacklogBlazor.Models;
using Microsoft.AspNetCore.Components;

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
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", authToken);
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

    public bool RequireAuthentication(NavigationManager nav)
    {
        if (!User.IsAuthenticated)
        {
            nav.NavigateTo("unauthorized");
            return false;
        }

        return true;
    }

    private async Task<long> GetNextBacklogId()
    {
        return await _httpClient.GetFromJsonAsync<long>("backlog/next");
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

        nav.NavigateTo($"backlog/{newId}?edit=true");
    }
    
    public async Task<BacklogModel> GetBacklog(long backlogId)
    {
        return await _httpClient.GetFromJsonAsync<BacklogModel>($"backlog/{backlogId}");
    }

    public async Task<List<BacklogModel>> GetUserBacklogs()
    {
        return await _httpClient.GetFromJsonAsync<List<BacklogModel>>("Backlog/list");
    }
}