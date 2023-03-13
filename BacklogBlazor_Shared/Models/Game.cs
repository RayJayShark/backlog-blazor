using System.Text.Json.Serialization;

namespace BacklogBlazor_Shared.Models;

public class Game
{
    [JsonPropertyName("game_id")]
    public long Id { get; set; } = -1;
    [JsonIgnore] 
    public long BacklogId { get; set; } = -1;
    [JsonPropertyName("game_name")]
    public string Name { get; set; }
    [JsonPropertyName("game_image")]
    public string GameImage { get; set; }
    [JsonIgnore]
    public string GameImageUrl => $"https://howlongtobeat.com/games/{GameImage}";
    public int Rank { get; set; }
    [JsonPropertyName("comp_main")]
    public int CompleteMainSeconds
    {
        get => (int) CompleteMainTime.TotalSeconds;
        set => CompleteMainTime = TimeSpan.FromSeconds(value);
    }
    [JsonIgnore]
    public TimeSpan CompleteMainTime { get; set; }
    [JsonPropertyName("comp_plus")]
    public int CompletePlusSeconds
    {
        get => (int) CompletePlusTime.TotalSeconds;
        set => CompletePlusTime = TimeSpan.FromSeconds(value);
    }
    [JsonIgnore]
    public TimeSpan CompletePlusTime { get; set; }
    [JsonPropertyName("comp_100")]
    public int Complete100Seconds
    {
        get => (int) Complete100Time.TotalSeconds;
        set => Complete100Time = TimeSpan.FromSeconds(value);
    }
    [JsonIgnore]
    public TimeSpan Complete100Time { get; set; }
    [JsonPropertyName("comp_all")]
    public int CompleteAllSeconds
    {
        get => (int) CompleteAllTime.TotalSeconds;
        set => CompleteAllTime = TimeSpan.FromSeconds(value);
    }
    [JsonIgnore]
    public TimeSpan CompleteAllTime { get; set; }
    public double EstimateCompleteHours { get; set; }
    public double CurrentHours { get; set; }
    public bool Completed { get; set; }
}