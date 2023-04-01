using System.Text.Json.Serialization;

namespace BacklogBlazor_Server.Models.ThirdPartyAuth;

public class DiscordUser
{
    public ulong Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public int Discriminator { get; set; }
    public string? Avatar { get; set; }
    public string? AvatarUrl => Avatar is not null ? $"https://cdn.discordapp.com/avatars/{Avatar}" : null;
    public int Flags { get; set; }
    public string? Banner { get; set; }
    public bool Bot { get; set; }
    
    [JsonPropertyName("accent_color")]
    public int? AccentColor { get; set; }

    [JsonPropertyName("premium_type")]
    public int PremiumType { get; set; }
    
    [JsonPropertyName("public_flags")]
    public int PublicFlags { get; set; }
}