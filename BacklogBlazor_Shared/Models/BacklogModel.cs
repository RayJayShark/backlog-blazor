namespace BacklogBlazor_Shared.Models;

public class BacklogModel
{
    public long Id { get; set; } = -1;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<Game> Games { get; set; } = new ();
    public List<Game> CompletedGames { get; set; } = new ();
    
    // Used by frontend for permissions
    public bool IsOwner { get; set; } = false;
}