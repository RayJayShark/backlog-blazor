using System.Net;
using System.Net.Http.Json;
using BacklogBlazor_Shared.Models;
using BacklogBlazor.Icons;
using BacklogBlazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BacklogBlazor.Shared;

public partial class BacklogComponent : ComponentBase
{
    [Parameter]
    public BacklogModel Backlog { get; set; }
    
    [Parameter]
    public EventCallback EditClicked { get; set; }

    [Parameter] 
    public string Class { get; set; }
    
    [Inject] private HttpClient HttpClient { get; set; }
    [Inject] private NotificationService NotificationService { get; set; }

    private HeroIcons[] _refreshIcons;
    private bool _disableRefresh = false;

    protected override void OnInitialized()
    {
        _refreshIcons = new HeroIcons[Backlog.Games.Count + 1];
    }

    private async Task RefreshGamesData(int iconToSpin, params Game[] games)
    {
        _disableRefresh = true;
        await _refreshIcons[iconToSpin].Spin();
        
        try
        {
            var response = await HttpClient.PostAsJsonAsync("games/refreshCache", games);

            var updatedGames = await response.Content.ReadFromJsonAsync<List<Game>>();

            foreach (var game in updatedGames)
            {
                var index = Backlog.Games.FindIndex(g => g.Id == game.Id);
                Backlog.Games[index] = game;
            }
        }
        catch (HttpRequestException ex)
        {
            switch (ex.StatusCode)
            {
                case HttpStatusCode.InternalServerError:
                    await NotificationService.DisplayNotification($"Error refreshing game data: Server error", NotificationLevel.Error);
                    break;
                default:
                    await NotificationService.DisplayNotification(
                        $"Error refreshing game data: Unable to connect to server", NotificationLevel.Error);
                    break;
            }
        }
        catch (Exception ex)
        {
            await NotificationService.DisplayNotification("Error refreshing game data: Unknown error", NotificationLevel.Error);
        }

        _disableRefresh = false;
        await _refreshIcons[iconToSpin].StopSpin();
    }
}