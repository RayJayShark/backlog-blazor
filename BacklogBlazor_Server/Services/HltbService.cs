using System.Net.Http.Headers;
using System.Text.Json;
using BacklogBlazor_Server.Models;
using BacklogBlazor_Shared.Models;

namespace BacklogBlazor_Server.Services;

public class HltbService
{
    private readonly HttpClient _httpClient;

    public HltbService()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://howlongtobeat.com/api/");
        _httpClient.DefaultRequestHeaders.Referrer = new Uri("https://howlongtobeat.com/");
        _httpClient.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("Other"));
    }

    public async Task<List<Game>> GetGamesFromSearch(string searchText)
    {
        var search = new HltbSearch
        {
            SearchType = "games",
            SearchTerms = searchText.Split(' ').ToList(),
            SearchPage = 1,
            Size = 20,
            SearchOptions = new HtlbSearchOptions
            {
                Games = new HltbSearchGames
                {
                    UserId = 0,
                    Platform = "",
                    SortCategory = "popular",
                    RangeCategory = "main",
                    RangeTime = new HltbRangeTime
                    {
                        Min = 0,
                        Max = 0
                    },
                    Gameplay = new HltbSearchGameplay
                    {
                        Perspective = "",
                        Flow = "",
                        Genre = ""
                    },
                    Modifier = ""
                },
                Users = new HltbSearchUsers{ SortCategory = "postcount" },
                Filter = "",
                Sort = 0,
                Randomizer = 0
            }
        };

        var response = await _httpClient.PostAsJsonAsync("search", search);

        if (!response.IsSuccessStatusCode)
        {
            return new List<Game>();
        }

        var responseObj = await response.Content.ReadFromJsonAsync<HltbResponse>();
        return responseObj.Data;
    }
}