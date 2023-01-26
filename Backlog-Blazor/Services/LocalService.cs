using Blazored.LocalStorage;

namespace BacklogBlazor.Services;

public class LocalService
{
    private readonly ILocalStorageService _localStorage;
    private readonly ISyncLocalStorageService _syncLocalStorage;

    public LocalService(ILocalStorageService localStorage, ISyncLocalStorageService syncLocalStorage)
    {
        _localStorage = localStorage;
        _syncLocalStorage = syncLocalStorage;
    }
    
    public async Task SetJwtTokenAsync(string token)
    {
        await _localStorage.SetItemAsStringAsync("authToken", token);
    }

    public async Task<string> GetJwtTokenAsync()
    {
        return await _localStorage.GetItemAsStringAsync("authToken");
    }

    public void RemoveJwtToken()
    {
        _syncLocalStorage.RemoveItem("authToken");
    }
    
    public string GetJwtToken()
    {
        return _syncLocalStorage.GetItemAsString("authToken");
    }
}