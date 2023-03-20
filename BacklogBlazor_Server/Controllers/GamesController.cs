using BacklogBlazor_Shared.Models;
using BacklogBlazor_Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace BacklogBlazor_Server.Controllers;

[ApiController]
[Route("[controller]")]
public class GamesController : Controller
{
    private readonly HltbService _hltbService;
    private readonly BacklogDataService _backlogDataService;

    public GamesController(HltbService hltbService, BacklogDataService backlogDataService)
    {
        _hltbService = hltbService;
        _backlogDataService = backlogDataService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> GameSearch([FromQuery] string searchText)
    {
        var games = new List<Game>();

        try
        {
            games = await _hltbService.GetGamesFromSearch(searchText);
        }
        catch (Exception ex)
        {
            // TODO: Log error
            Console.WriteLine(ex.Message);
            return StatusCode(500, games);
        }

        return Ok(games);
    }

    [HttpPost("refreshCache")]
    [Consumes("application/json")]
    public async Task<IActionResult> RefreshCache([FromBody] List<Game> games)
    {
        if (!games.Any())
            return Ok();

        var gamesToCache = games.Select(game =>
        {
            var hltbGames = _hltbService.GetGamesFromSearch(game.Name).Result;
            var gameData = hltbGames.FirstOrDefault(hltbG => hltbG.Id == game.Id);
            return new Game
            {
                Id = game.Id,
                Name = game.Name,
                GameImage = gameData.GameImage,
                Rank = game.Rank,
                CompleteMainSeconds = gameData.CompleteMainSeconds,
                CompletePlusSeconds = gameData.CompletePlusSeconds,
                Complete100Seconds = gameData.Complete100Seconds,
                CompleteAllSeconds = gameData.CompleteAllSeconds,
                EstimateCompleteHours = game.EstimateCompleteHours,
                CurrentHours = game.CurrentHours,
                Completed = game.Completed
            };
        }).ToList();

        await _backlogDataService.CacheGames(gamesToCache);
        
        return Ok(gamesToCache);
    }
}