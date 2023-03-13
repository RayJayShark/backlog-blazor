﻿using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Web;
using BacklogBlazor_Shared.Models;
using BacklogBlazor.Models;
using BacklogBlazor.Services;
using Microsoft.AspNetCore.Components;

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
    [Inject] private NotificationService NotificationService { get; set; }

    private event Action BacklogChanged;
    private List<TypeaheadGame> _typeaheadGames = new();
    private bool _disableSave = false;
    private string _notification;

    public BacklogEdit()
    {
        BacklogChanged = OnBacklogChanged;
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
        for (var i = gameIndex; i < clampedRank - 1; i++)
        {
            _typeaheadGames[i].Game.Rank--;
        }
    }

    private void AddGame()
    {
        var typeaheadGame = new TypeaheadGame();
        typeaheadGame.Game.Rank = _typeaheadGames.Count + 1;
        _typeaheadGames.Add(typeaheadGame);
    }

    private async Task SaveBacklog()
    {
        _disableSave = true;
        StateHasChanged();
        
        // _typeaheadGames needs to be inserted into the Backlog.Games list
        _backlog.Games = _typeaheadGames.Select(tg => tg.Game).ToList();

        HttpResponseMessage response = null;
        try
        {
            response = await HttpClient.PostAsJsonAsync("backlog", _backlog);
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

        if (response is not null && response.IsSuccessStatusCode)
        {
            await NotificationService.DisplayNotification("Backlog saved!", NotificationLevel.Success);
            DoneEditing.InvokeAsync();
        }

        _disableSave = false;
        StateHasChanged();
    }
}