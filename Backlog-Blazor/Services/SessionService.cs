using Blazored.SessionStorage;

namespace BacklogBlazor.Services;

public class SessionService
{
    private readonly ISessionStorageService _sessionStorage;
    private readonly ISyncSessionStorageService _syncSessionStorage;

    public SessionService(ISessionStorageService sessionStorage, ISyncSessionStorageService syncSessionStorage)
    {
        _sessionStorage = sessionStorage;
        _syncSessionStorage = syncSessionStorage;
    }
    
    public async Task SetJwtTokenAsync(string token)
    {
        await _sessionStorage.SetItemAsStringAsync("authToken", token);
    }

    public async Task<string> GetJwtTokenAsync()
    {
        return await _sessionStorage.GetItemAsStringAsync("authToken");
    }
    
    public void RemoveJwtToken()
    {
        _syncSessionStorage.RemoveItem("authToken");
    }

    public string GetJwtToken()
    {
        return _syncSessionStorage.GetItemAsString("authToken");
    }
}