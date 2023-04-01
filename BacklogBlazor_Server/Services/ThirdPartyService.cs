using System.Net.Http.Headers;
using BacklogBlazor_Server.Models;
using BacklogBlazor_Server.Models.ThirdPartyAuth;

namespace BacklogBlazor_Server.Services;

public class ThirdPartyService
{
    private readonly HttpClient _httpClient;
    private readonly string _discordClient;
    private readonly string _discordSecret;
    private const string DISCORD_API = "https://discord.com/api/v10/";

    public ThirdPartyService()
    {
        _discordClient = Environment.GetEnvironmentVariable("DISCORD_CLIENT");
        _discordSecret = Environment.GetEnvironmentVariable("DISCORD_SECRET");
        _httpClient = new HttpClient();
    }

    public async Task<string> AuthenticateDiscordCode(string code)
    {
        var content = new Dictionary<string, string>
        {
            { "client_id", _discordClient },
            { "client_secret", _discordSecret },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", "https://localhost:7153/login/discord" }
        };

        var response = await _httpClient.PostAsync(DISCORD_API + "oauth2/token", new FormUrlEncodedContent(content));

        if (!response.IsSuccessStatusCode)
            return string.Empty;

        var authData = await response.Content.ReadFromJsonAsync<DiscordAuthResponse>();

        return authData.AccessToken;
    }

    public async Task<DiscordUser?> GetDiscordUserData(string accessToken)
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri(DISCORD_API + "users/@me"),
            Method = HttpMethod.Get
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<DiscordUser>();
    }
}