namespace PrezziarioOOEELombardia.Shared;

public class TreeNodeDTO
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Level { get; set; }
    public bool HasChildren { get; set; }
    public List<TreeNodeDTO>? Children { get; set; }
}
