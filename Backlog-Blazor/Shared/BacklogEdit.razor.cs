using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Web;
using BacklogBlazor_Shared.Models;
using BacklogBlazor.Icons;
using BacklogBlazor.Models;
using BacklogBlazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BacklogBlazor.Shared;

public partial class BacklogEdit : ComponentBase
{

    private BacklogModel _backlog = new();
    [Parameter] 
    public BacklogModel Backlog
    {
        get => _backlog;
        set
        {
            _backlog = value;
            BacklogChanged.Invoke();
        }
    }
    
    [Parameter]
    public EventCallback DoneEditing { get; set; }

    [Parameter] 
    public string Class { get; set; }

    [Inject] private HttpClient HttpClient { get; set; }
    [Inject] private AuthorizedApiService AuthorizedApiService { get; set; }
    [Inject] private NotificationService NotificationService { get; set; }
    [Inject] private NavigationManager Nav { get; set; }
    [Inject] private IJSRuntime JsRuntime { get; set; }

    private event Action BacklogChanged;
    private List<TypeaheadGame> _typeaheadGames = new();
    private bool _disableSave = false;
    private HeroIcons[] _refreshIcons;
    private bool _disableRefresh = false;

    public BacklogEdit()
    {
        BacklogChanged = OnBacklogChanged;
    }

    protected override void OnInitialized()
    {
        _refreshIcons = new HeroIcons[_typeaheadGames.Count + 1];
    }

    private void OnBacklogChanged()
    {
        if (_backlog.Games is not null)
        {
            _typeaheadGames = _backlog.Games.Select(g => new TypeaheadGame { Game = g }).ToList();
        }
    }

    private async Task<IEnumerable<Game>> SearchForGames(string searchText, int rank)
    {
        var gameList = new List<Game>();
        try
        {
            gameList =
                await HttpClient.GetFromJsonAsync<List<Game>>(
                    $"games/search?searchText={HttpUtility.UrlEncode(searchText)}");
        }
        catch (HttpRequestException ex)
        {
            switch (ex.StatusCode)
            {
                case HttpStatusCode.InternalServerError:
                    await NotificationService.DisplayNotification($"Error searching for game '{searchText}': Server error", NotificationLevel.Error);
                    break;
                default:
                    await NotificationService.DisplayNotification(
                        $"Error searching for game '{searchText}': Unable to connect to server", NotificationLevel.Error);
                    break;
            }
        }
        catch (Exception ex)
        {
            await NotificationService.DisplayNotification("Unknown error", NotificationLevel.Error);
        }

        return gameList.Select(game =>
        {
            game.Rank = rank;
            return game;
        }); 
    }

    private void RankChanged(int gameIndex, int rank)
    {
        var clampedRank = int.Clamp(rank, 1, _typeaheadGames.Count);
        _typeaheadGames[gameIndex].Game.Rank = clampedRank;

        if (gameIndex + 1 == clampedRank)
            return;

        // Move game to the correct spot in the list
        var game = _typeaheadGames[gameIndex];
        _typeaheadGames.RemoveAt(gameIndex);
        _typeaheadGames.Insert(clampedRank - 1, game);
        
        // Shift all necessary game ranks
        // If moved forward
        if (rank > gameIndex + 1)
        {
            for (var i = gameIndex; i < clampedRank - 1; i++)
            {
                _typeaheadGames[i].Game.Rank--;
            }
        }
        // If moved backward
        else
        {
            for (var i = clampedRank; i < gameIndex + 1; i++)
            {
                _typeaheadGames[i].Game.Rank++;
            }
        }
    }

    private void AddGame()
    {
        var typeaheadGame = new TypeaheadGame();
        typeaheadGame.Game.Rank = _typeaheadGames.Count + 1;
        typeaheadGame.DisableTypeahead = false;
        _typeaheadGames.Add(typeaheadGame);

        var newRefreshIcons = new HeroIcons[_typeaheadGames.Count + 1];
        for (var i = 0; i < _refreshIcons.Length; i++)
            newRefreshIcons[i] = _refreshIcons[i];

        _refreshIcons = newRefreshIcons;
    }

    private void RemoveGame(int rank)
    {
        var index = rank - 1;
         _typeaheadGames.RemoveAt(index);

         for (var i = index; i < _typeaheadGames.Count; i++)
         {
             _typeaheadGames[i].Game.Rank--;
         }
    }

    private async Task SaveBacklog()
    {
        _disableSave = true;
        StateHasChanged();

        if (_typeaheadGames.Any(g => g.Game.Id < 0))
        {
            await NotificationService.DisplayNotification("Cannot save backlog: Please populate all games before saving", NotificationLevel.Warning);
            
            _disableSave = false;
            StateHasChanged();
            return;
        }
        
        // _typeaheadGames needs to be inserted into the Backlog.Games list
        _backlog.Games = _typeaheadGames.Select(tg => tg.Game).ToList();
        
        try
        {
            await AuthorizedApiService.SaveBacklog(_backlog);
            
            await NotificationService.DisplayNotification("Backlog saved!", NotificationLevel.Success);
            DoneEditing.InvokeAsync();
        }
        catch (HttpRequestException ex)
        {
            switch (ex.StatusCode)
            {
                case HttpStatusCode.InternalServerError:
                    await NotificationService.DisplayNotification("Error when saving backlog: Server error",
                        NotificationLevel.Error);
                    break;
                case HttpStatusCode.BadRequest:
                    await NotificationService.DisplayNotification("Error when saving backlog: Invalid backlog ID",
                        NotificationLevel.Error);
                    break;
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    await NotificationService.DisplayNotification("Error when saving backlog: Unable to authenticate",
                        NotificationLevel.Error);
                    break;
                default:
                    await NotificationService.DisplayNotification("Error when saving backlog: Unable to connect to server",
                        NotificationLevel.Error);
                    break;
            }
        }
        
        _disableSave = false;
        StateHasChanged();
    }

    private async Task DisplayBacklogConfirmation()
    {
        var alertResult = await JsRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this backlog?");

        if (alertResult)
            await DeleteBacklog();
    }
    
    private async Task DeleteBacklog()
    {
        _disableSave = true;
        StateHasChanged();
        
        try
        {
            await AuthorizedApiService.DeleteBacklog(Backlog.Id);

            await NotificationService.DisplayNotification("Backlog deleted", NotificationLevel.Info);
            Nav.NavigateTo("backlog/list");
        }
        catch (HttpRequestException ex)
        {
            switch (ex.StatusCode)
            {
                case HttpStatusCode.InternalServerError:
                    await NotificationService.DisplayNotification("Error when deleting backlog: Server error",
                        NotificationLevel.Error);
                    break;
                case HttpStatusCode.BadRequest:
                    await NotificationService.DisplayNotification("Error when deleting backlog: Invalid backlog ID",
                        NotificationLevel.Error);
                    break;
                case HttpStatusCode.Unauthorized:
                case HttpStatusCode.Forbidden:
                    await NotificationService.DisplayNotification("Error when deleting backlog: Unable to authenticate",
                        NotificationLevel.Error);
                    break;
                default:
                    await NotificationService.DisplayNotification("Error when deleting backlog: Unable to connect to server",
                        NotificationLevel.Error);
                    break;
            }
        }
        
        _disableSave = false;
        StateHasChanged();
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
                var index = _typeaheadGames.FindIndex(g => g.Game.Id == game.Id);
                _typeaheadGames[index].Game = game;
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