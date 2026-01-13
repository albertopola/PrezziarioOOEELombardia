using System.Xml;
using Microsoft.EntityFrameworkCore;
using PrezziarioOOEELombardia.Server.Data;

namespace PrezziarioOOEELombardia.Server.Services;

public class XmlParserService
{
    private readonly PrezziarioDbContext _context;
    private readonly ILogger<XmlParserService> _logger;
    private readonly string _xmlFilePath;

    public XmlParserService(
        PrezziarioDbContext context,
        ILogger<XmlParserService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _xmlFilePath = configuration["XmlFilePath"] ?? "Data/prezziario.xml";
    }

    public async Task<bool> IsDatabaseInitializedAsync()
    {
        return await _context.Voci.AnyAsync();
    }

    public async Task InitializeDatabaseAsync()
    {
        _logger.LogInformation("Starting database initialization from XML file: {XmlFile}", _xmlFilePath);

        if (!File.Exists(_xmlFilePath))
        {
            _logger.LogError("XML file not found: {XmlFile}", _xmlFilePath);
            throw new FileNotFoundException($"XML file not found: {_xmlFilePath}");
        }

        // Clear existing data
        _context.Risorse.RemoveRange(_context.Risorse);
        _context.Voci.RemoveRange(_context.Voci);
        await _context.SaveChangesAsync();

        var settings = new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        using var reader = XmlReader.Create(_xmlFilePath, settings);
        var voci = new List<Voce>();
        var batchSize = 100;

        while (await reader.ReadAsync())
        {
            if (reader.NodeType == XmlNodeType.Element && reader.Name == "dettaglio_voce")
            {
                var voce = await ParseVoceAsync(reader);
                if (voce != null)
                {
                    voci.Add(voce);

                    if (voci.Count >= batchSize)
                    {
                        await _context.Voci.AddRangeAsync(voci);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Processed {Count} voci", voci.Count);
                        voci.Clear();
                    }
                }
            }
        }

        // Save remaining voci
        if (voci.Any())
        {
            await _context.Voci.AddRangeAsync(voci);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Processed final {Count} voci", voci.Count);
        }

        _logger.LogInformation("Database initialization completed");
    }

    private async Task<Voce?> ParseVoceAsync(XmlReader reader)
    {
        var voce = new Voce();
        var depth = reader.Depth;

        try
        {
            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "dettaglio_voce")
                {
                    break;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    var elementName = reader.Name;
                    var value = await reader.ReadElementContentAsStringAsync();

                    switch (elementName)
                    {
                        case "codice_voce": voce.CodiceVoce = value; break;
                        case "prezzo_voce": voce.PrezzoVoce = ParseDecimal(value); break;
                        case "unita_misura_voce": voce.UnitaMisuraVoce = value; break;
                        case "importo_senza_sgui_voce": voce.ImportoSenzaSguiVoce = ParseDecimal(value); break;
                        case "rapporto_RU_voce": voce.RapportoRUVoce = ParseDecimal(value); break;
                        case "tipologia_risorsa": voce.TipologiaRisorsa = value; break;
                        case "declaratoria_voce": voce.DeclaratoriaVoce = value; break;
                        case "declaratoria_voce_dettaglio": voce.DeclaratoriaVoceDettaglio = value; break;
                        
                        // Livelli gerarchici
                        case "cod_liv_1": voce.CodLiv1 = value; break;
                        case "descr_liv_1": voce.DescrLiv1 = value; break;
                        case "cod_liv_2": voce.CodLiv2 = value; break;
                        case "descr_liv_2": voce.DescrLiv2 = value; break;
                        case "cod_liv_3": voce.CodLiv3 = value; break;
                        case "descr_liv_3": voce.DescrLiv3 = value; break;
                        case "cod_liv_4": voce.CodLiv4 = value; break;
                        case "descr_liv_4": voce.DescrLiv4 = value; break;
                        case "cod_liv_5": voce.CodLiv5 = value; break;
                        case "descr_liv_5": voce.DescrLiv5 = value; break;
                        case "cod_liv_6": voce.CodLiv6 = value; break;
                        case "descr_liv_6": voce.DescrLiv6 = value; break;
                        case "cod_liv_7": voce.CodLiv7 = value; break;
                        case "descr_liv_7": voce.DescrLiv7 = value; break;
                        case "cod_liv_8": voce.CodLiv8 = value; break;
                        case "descr_liv_8": voce.DescrLiv8 = value; break;
                        case "cod_liv_9": voce.CodLiv9 = value; break;
                        case "descr_liv_9": voce.DescrLiv9 = value; break;
                        case "cod_liv_10": voce.CodLiv10 = value; break;
                        case "descr_liv_10": voce.DescrLiv10 = value; break;
                        case "cod_liv_11": voce.CodLiv11 = value; break;
                        case "descr_liv_11": voce.DescrLiv11 = value; break;

                        case "dettaglio_risorsa":
                            var risorsa = await ParseRisorsaAsync(reader);
                            if (risorsa != null)
                            {
                                voce.Risorse.Add(risorsa);
                            }
                            break;
                    }
                }
            }

            return voce;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing voce");
            return null;
        }
    }

    private async Task<Risorsa?> ParseRisorsaAsync(XmlReader reader)
    {
        var risorsa = new Risorsa();

        try
        {
            while (await reader.ReadAsync())
            {
                if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "dettaglio_risorsa")
                {
                    break;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    var elementName = reader.Name;
                    var value = await reader.ReadElementContentAsStringAsync();

                    switch (elementName)
                    {
                        case "codifica_risorsa": risorsa.CodificaRisorsa = value; break;
                        case "udm_risorsa": risorsa.UdmRisorsa = value; break;
                        case "quantita_risorsa": risorsa.QuantitaRisorsa = ParseDecimal(value); break;
                        case "prezzo_risorsa": risorsa.PrezzoRisorsa = ParseDecimal(value); break;
                        case "importo_risorsa": risorsa.ImportoRisorsa = ParseDecimal(value); break;
                        case "tipologia_risorsa": risorsa.TipologiaRisorsa = value; break;
                        case "declaratoria_risorsa": risorsa.DeclaratoriaRisorsa = value; break;
                    }
                }
            }

            return risorsa;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing risorsa");
            return null;
        }
    }

    private static decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        value = value.Replace(',', '.');
        return decimal.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}
