namespace BacklogBlazor_Server.Models;

public class User
{
    public long UserId { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string AvatarUrl { get; set; }
    public ulong DiscordId { get; set; }
}