namespace BacklogBlazor_Server.Models;

public class HltbSearch
{
    public string SearchType { get; set; }
    public List<string> SearchTerms { get; set; }
    public int SearchPage { get; set; }
    public int Size { get; set; }
    public HtlbSearchOptions SearchOptions { get; set; }
}

public class HtlbSearchOptions
{
    public HltbSearchGames Games { get; set; }
    public HltbSearchUsers Users { get; set; }
    public string Filter { get; set; }
    public int Sort { get; set; }
    public int Randomizer { get; set; }
}

public class HltbSearchGames
{
    public int UserId { get; set; }
    public string Platform { get; set; }
    public string SortCategory { get; set; }
    public string RangeCategory { get; set; }
    public HltbRangeTime RangeTime { get; set; }
    public HltbSearchGameplay Gameplay { get; set; }
    public string Modifier { get; set; }
}

public class HltbRangeTime
{
    public int Min { get; set; }
    public int Max { get; set; }
}

public class HltbSearchGameplay
{
    public string Perspective { get; set; }
    public string Flow { get; set; }
    public string Genre { get; set; }
}

public class HltbSearchUsers
{
    public string SortCategory { get; set; }
}