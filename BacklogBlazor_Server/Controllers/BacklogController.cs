using BacklogBlazor_Server.Services;
using BacklogBlazor_Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BacklogBlazor_Server.Controllers;

[ApiController]
[Route("[controller]")]
public class BacklogController : Controller
{
    private readonly BacklogDataService _backlogDataService;
    private readonly HltbService _hltbService;
    private readonly AuthDataService _authDataService;

    public BacklogController(BacklogDataService backlogDataService, HltbService hltbService, AuthDataService authDataService)
    {
        _backlogDataService = backlogDataService;
        _hltbService = hltbService;
        _authDataService = authDataService;
    }
    
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> SaveBacklog([FromBody] BacklogModel backlog)
    {
        if (backlog.Id < 0)
            return BadRequest();

        var userId = await GetUserId();
        if (userId < 0)
            return Forbid();

        if (!await _backlogDataService.IsOwner(backlog.Id, userId))
            return Forbid();
        
        //TODO: Any other validation (valid game, valid user, etc.)
        
        var success = await _backlogDataService.UpdateBacklogData(backlog);

        if (!success)
            return StatusCode(500, "Error updating backlog");

        backlog.Games.ForEach(g => g.BacklogId = backlog.Id);
        backlog.CompletedGames.ForEach(g => g.BacklogId = backlog.Id);
        success = await _backlogDataService.SaveBacklogItems(backlog);

        if (!success)
            return StatusCode(500, "Error updating backlog games");

        await _backlogDataService.CacheGames(backlog.Games);
        
        return Ok();
    }

    [Authorize]
    [AllowAnonymous]
    [HttpGet("{backlogId:long}")]
    public async Task<IActionResult> GetBacklog(long backlogId)
    {
        if (backlogId < 0)
            return BadRequest("Invalid Backlog ID");

        var backlog = await _backlogDataService.GetBacklog(backlogId);

        var userId = await GetUserId();

        if (userId >= 0 && await _backlogDataService.IsOwner(backlogId, userId))
            backlog.IsOwner = true;
        
        backlog.Games = backlog.Games.Select(g =>
        {
            if (g.CompleteAllSeconds > 0) // Data retrieved from cache
                return g;
            
            var hltbGames = _hltbService.GetGamesFromSearch(g.Name).Result;
            var gameData = hltbGames.FirstOrDefault(hltbG => hltbG.Id == g.Id);
            return new Game
            {
                Id = g.Id,
                Name = g.Name,
                GameImage = gameData?.GameImage ?? string.Empty,
                Rank = g.Rank,
                CompleteMainSeconds = gameData?.CompleteMainSeconds ?? 0,
                CompletePlusSeconds = gameData?.CompletePlusSeconds ?? 0,
                Complete100Seconds = gameData?.Complete100Seconds ?? 0,
                CompleteAllSeconds = gameData?.CompleteAllSeconds ?? 0,
                EstimateCompleteHours = g.EstimateCompleteHours,
                CurrentHours = g.CurrentHours,
            };
        }).ToList();
        
        backlog.CompletedGames = backlog.CompletedGames.Select(g =>
        {
            if (g.CompleteAllSeconds > 0) // Data retrieved from cache
                return g;
            
            var hltbGames = _hltbService.GetGamesFromSearch(g.Name).Result;
            var gameData = hltbGames.FirstOrDefault(hltbG => hltbG.Id == g.Id);
            return new Game
            {
                Id = g.Id,
                Name = g.Name,
                GameImage = gameData.GameImage,
                Rank = g.Rank,
                CompleteMainSeconds = gameData.CompleteMainSeconds,
                CompletePlusSeconds = gameData.CompletePlusSeconds,
                Complete100Seconds = gameData.Complete100Seconds,
                CompleteAllSeconds = gameData.CompleteAllSeconds,
                EstimateCompleteHours = g.EstimateCompleteHours,
                CurrentHours = g.CurrentHours,
            };
        }).ToList();

        return Ok(backlog);
    }

    [Authorize]
    [HttpGet("list")]
    public async Task<IActionResult> GetUserBacklogs()
    {
        var userId = await GetUserId();

        if (userId < 0)
            return Forbid();

        var backlogs = await _backlogDataService.GetUserBacklogs(userId);

        return Ok(backlogs);
    }

    [Authorize]
    [HttpGet("recent")]
    public async Task<IActionResult> GetUserRecentBacklogs()
    {
        var userId = await GetUserId();

        if (userId < 0)
            return Forbid();

        var recentBacklogs = await _backlogDataService.GetUserRecentBacklogs(userId);

        return Ok(recentBacklogs);
    }
    
    [Authorize]
    [HttpGet("next")]
    public async Task<IActionResult> GetNextBacklogId()
    {
        var userId = await GetUserId();

        if (userId < 0)
            return Forbid();
        
        var newId = await _backlogDataService.GetNextBacklogId(userId);

        if (newId < 0)
            return StatusCode(500, "Error inserting new backlog");
        
        return Ok(newId);
    }

    [Authorize]
    [HttpDelete("{backlogId:long}")]
    public async Task<IActionResult> DeleteBacklog(long backlogId)
    {
        if (backlogId < 0)
            return BadRequest();

        var userId = await GetUserId();
        if (userId < 0)
            return Forbid();

        if (!await _backlogDataService.IsOwner(backlogId, userId))
            return Forbid();

        var success = await _backlogDataService.DeleteBacklog(backlogId);

        return success ? Ok() : StatusCode(500);
    }

    #region HelperMethods
    
    private async Task<long> GetUserId()
    {
        if (!User.HasClaim(c => c.Type == "userId"))
            return -1;

        var userIdString = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
        if (!long.TryParse(userIdString, out var userIdLong))
            return -1;

        if (!await _authDataService.IsValidUser(userIdLong))
            return -1;

        return userIdLong;
    }
    
    #endregion
}