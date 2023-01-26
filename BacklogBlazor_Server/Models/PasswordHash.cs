namespace BacklogBlazor_Server.Models;

public class PasswordHash
{
    public long UserId { get; set; }
    public string Username { get; set; }
    public string Hash { get; set; }
}