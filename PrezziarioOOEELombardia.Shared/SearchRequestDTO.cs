namespace PrezziarioOOEELombardia.Shared;

public class SearchRequestDTO
{
    public string SearchTerm { get; set; } = string.Empty;
    public int? Level { get; set; }
    public bool SearchFromEnd { get; set; } // Per ricerca dagli ultimi livelli
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
