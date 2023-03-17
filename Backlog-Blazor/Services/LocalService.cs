using Blazored.LocalStorage;

namespace BacklogBlazor.Services;

public class LocalService
{
    private readonly ILocalStorageService _localStorage;
    private readonly ISyncLocalStorageService _syncLocalStorage;

    private const string REFRESH_TOKEN_KEY = "refreshToken"; 

    public LocalService(ILocalStorageService localStorage, ISyncLocalStorageService syncLocalStorage)
    {
        _localStorage = localStorage;
        _syncLocalStorage = syncLocalStorage;
    }

    public void SetRefreshToken(string token)
    {
        _syncLocalStorage.SetItemAsString(REFRESH_TOKEN_KEY, token);
    }
    
    public async Task SetRefreshTokenAsync(string token)
    {
        await _localStorage.SetItemAsStringAsync(REFRESH_TOKEN_KEY, token);
    }

    public async Task<string> GetRefreshTokenAsync()
    {
        return await _localStorage.GetItemAsStringAsync(REFRESH_TOKEN_KEY);
    }

    public void RemoveRefreshToken()
    {
        _syncLocalStorage.RemoveItem(REFRESH_TOKEN_KEY);
    }
    
    public string GetRefreshToken()
    {
        return _syncLocalStorage.GetItemAsString(REFRESH_TOKEN_KEY);
    }
}