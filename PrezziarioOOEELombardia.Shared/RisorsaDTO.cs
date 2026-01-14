namespace PrezziarioOOEELombardia.Shared;

public class RisorsaDTO
{
    public string CodificaRisorsa { get; set; } = string.Empty;
    public string UdmRisorsa { get; set; } = string.Empty;
    public decimal QuantitaRisorsa { get; set; }
    public decimal PrezzoRisorsa { get; set; }
    public decimal ImportoRisorsa { get; set; }
    public string TipologiaRisorsa { get; set; } = string.Empty;
    public string DeclaratoriaRisorsa { get; set; } = string.Empty;
}
