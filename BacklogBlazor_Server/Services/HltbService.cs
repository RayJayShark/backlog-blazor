using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using BacklogBlazor_Server.Models;
using BacklogBlazor_Shared.Models;
using HostingEnvironmentExtensions = Microsoft.AspNetCore.Hosting.HostingEnvironmentExtensions;

namespace BacklogBlazor_Server.Services;

public class HltbService
{
    private readonly HttpClient _httpClient;
    private ILogger<HltbService> _logger { get; set; }

    public HltbService(ILogger<HltbService> logger)
    {
        _logger = logger;
        
        var baseUri = new Uri("https://howlongtobeat.com/");
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = baseUri;
        _httpClient.DefaultRequestHeaders.Referrer = baseUri;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:128.0) Gecko/20100101 Firefox/128.0");
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

        var apiKey = await GetApiKey();
        var response = await _httpClient.PostAsJsonAsync($"api/search/{apiKey}", search);

        // Try /find instead of /search
        if (!response.IsSuccessStatusCode)
        {
            response = await _httpClient.PostAsJsonAsync("api/find/" + GetApiKey(), search);
        }
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("HLTB returned with no games. Uri: {RequestUri}", response.RequestMessage?.RequestUri?.AbsoluteUri ?? string.Empty);
            return new List<Game>();
        }

        var responseObj = await response.Content.ReadFromJsonAsync<HltbResponse>();
        return responseObj.Data;
    }

    /// <summary>
    /// Gets the key/hash/whatever that is appended to the end of the search endpoint
    /// </summary>
    /// <returns>API key</returns>
    private async Task<string> GetApiKey()
    {
        // Get base HTML
        var baseResponse = await _httpClient.GetAsync("");
        if (!baseResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to call base HLTB URL");
            return string.Empty;
        }

        // Find the _app javascript file
        var html = await baseResponse.Content.ReadAsStringAsync();
        var jsFileMatch = Regex.Match(html, "src=\"(\\/[\\w\\/]+\\/_app-[a-z0-9]+\\.js)\"");
        if (!jsFileMatch.Success)
        {
            _logger.LogWarning("Could not find _app script in HTML");
            return string.Empty;
        }

        // Get the contents of the javascript file
        var jsFileLocation = jsFileMatch.Groups[1].Value;
        var jsResponse = await _httpClient.GetAsync(jsFileLocation);
        if (!jsResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to call JS file URL: {JsFile}", jsFileLocation);
            return string.Empty;
        }
        
        // Find the API key
        var jsFile = await jsResponse.Content.ReadAsStringAsync();
        var apiKeyMatch = Regex.Match(jsFile, """\/api\/(?:search|find)\/"\.concat\("([a-zA-Z0-9]+)"\)\.concat\("([a-zA-Z0-9]+)"\)""");
        if (!apiKeyMatch.Success)
        {
            _logger.LogWarning("Could not find API key in JavaScript file");
            return string.Empty;
        }

        return apiKeyMatch.Groups[1].Value + apiKeyMatch.Groups[2].Value;
    }
}