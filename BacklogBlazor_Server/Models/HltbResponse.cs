using BacklogBlazor_Shared.Models;

namespace BacklogBlazor_Server.Models;

public class HltbResponse
{
    public string Color { get; set; }
    public string Title { get; set; }
    public string Category { get; set; }
    public int? Count { get; set; }
    public int PageCurrent { get; set; }
    public int? PageTotal { get; set; }
    public int PageSize { get; set; }
    public List<Game> Data { get; set; }
}