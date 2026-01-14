namespace PrezziarioOOEELombardia.Shared;

public class SearchResultDTO
{
    public List<VoceDTO> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
}
