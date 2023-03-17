using Blazored.SessionStorage;

namespace BacklogBlazor.Services;

public class SessionService
{
    private readonly ISessionStorageService _sessionStorage;
    private readonly ISyncSessionStorageService _syncSessionStorage;

    private const string JWT_TOKEN_KEY = "authToken";
    private const string REFRESH_TOKEN_KEY = "refreshToken"; 

    public SessionService(ISessionStorageService sessionStorage, ISyncSessionStorageService syncSessionStorage)
    {
        _sessionStorage = sessionStorage;
        _syncSessionStorage = syncSessionStorage;
    }

    public void SetJwtToken(string token)
    {
        _syncSessionStorage.SetItemAsString(JWT_TOKEN_KEY, token);
    }
    
    public async Task SetJwtTokenAsync(string token)
    {
        await _sessionStorage.SetItemAsStringAsync(JWT_TOKEN_KEY, token);
    }
    
    public void SetRefreshToken(string token)
    {
        _syncSessionStorage.SetItemAsString(REFRESH_TOKEN_KEY, token);
    }
    
    public async Task SetRefreshTokenAsync(string token)
    {
        await _sessionStorage.SetItemAsStringAsync(REFRESH_TOKEN_KEY, token);
    }

    public string GetJwtToken()
    {
        return _syncSessionStorage.GetItemAsString(JWT_TOKEN_KEY);
    }
    
    public async Task<string> GetJwtTokenAsync()
    {
        return await _sessionStorage.GetItemAsStringAsync(JWT_TOKEN_KEY);
    }

    public string GetRefreshToken()
    {
        return _syncSessionStorage.GetItemAsString(REFRESH_TOKEN_KEY);
    }
    
    public async Task<string> GetRefreshTokenAsync()
    {
        return await _sessionStorage.GetItemAsStringAsync(REFRESH_TOKEN_KEY);
    }
    
    public void RemoveJwtToken()
    {
        _syncSessionStorage.RemoveItem(JWT_TOKEN_KEY);
    }

    public void RemoveRefreshToken()
    {
        _syncSessionStorage.RemoveItem(REFRESH_TOKEN_KEY);
    }
}