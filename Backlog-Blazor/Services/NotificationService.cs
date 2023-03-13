using Microsoft.JSInterop;

namespace BacklogBlazor.Services;

public class NotificationService
{
    private readonly IJSRuntime _jsRuntime;

    public NotificationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task DisplayNotification(string text, NotificationLevel notificationLevel = NotificationLevel.Info)
    {
        await _jsRuntime.InvokeVoidAsync("displayNotification", text, notificationLevel);
    }
}

public enum NotificationLevel
{
    Info,
    Success,
    Warning,
    Error
}