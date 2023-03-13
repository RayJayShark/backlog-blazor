using BacklogBlazor_Shared.Models;
using BacklogBlazor_Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace BacklogBlazor_Server.Controllers;

[ApiController]
[Route("[controller]")]
public class GamesController : Controller
{
    private readonly HltbService _hltbService;

    public GamesController(HltbService hltbService)
    {
        _hltbService = hltbService;
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
}