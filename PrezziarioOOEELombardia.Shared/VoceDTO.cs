namespace PrezziarioOOEELombardia.Shared;

public class VoceDTO
{
    public string Autore { get; set; } = string.Empty;
    public string Anno { get; set; } = string.Empty;
    public string Edizione { get; set; } = string.Empty;
    public string CodiceVoce { get; set; } = string.Empty;
    public decimal PrezzoVoce { get; set; }
    public string UnitaMisuraVoce { get; set; } = string.Empty;
    public decimal ImportoSenzaSguiVoce { get; set; }
    public decimal RapportoRUVoce { get; set; }
    public string TipologiaRisorsa { get; set; } = string.Empty;
    public string DeclaratoriaVoce { get; set; } = string.Empty;
    public string DeclaratoriaVoceDettaglio { get; set; } = string.Empty;
    
    // Livelli gerarchici
    public string? CodLiv1 { get; set; }
    public string? DescrLiv1 { get; set; }
    public string? CodLiv2 { get; set; }
    public string? DescrLiv2 { get; set; }
    public string? CodLiv3 { get; set; }
    public string? DescrLiv3 { get; set; }
    public string? CodLiv4 { get; set; }
    public string? DescrLiv4 { get; set; }
    public string? CodLiv5 { get; set; }
    public string? DescrLiv5 { get; set; }
    public string? CodLiv6 { get; set; }
    public string? DescrLiv6 { get; set; }
    public string? CodLiv7 { get; set; }
    public string? DescrLiv7 { get; set; }
    public string? CodLiv8 { get; set; }
    public string? DescrLiv8 { get; set; }
    public string? CodLiv9 { get; set; }
    public string? DescrLiv9 { get; set; }
    public string? CodLiv10 { get; set; }
    public string? DescrLiv10 { get; set; }
    public string? CodLiv11 { get; set; }
    public string? DescrLiv11 { get; set; }
    
    public List<RisorsaDTO> Risorse { get; set; } = new();
}
