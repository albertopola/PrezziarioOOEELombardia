using Microsoft.EntityFrameworkCore;
using PrezziarioOOEELombardia.Server.Data;
using System.Diagnostics;
using System.IO;
using System.Xml;

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
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting database initialization from XML file: {XmlFile}", _xmlFilePath);

        if (!File.Exists(_xmlFilePath))
        {
            _logger.LogError("XML file not found: {XmlFile}", _xmlFilePath);
            throw new FileNotFoundException($"XML file not found:  {_xmlFilePath}");
        }

        // Log file size
        var fileInfo = new FileInfo(_xmlFilePath);
        _logger.LogInformation("XML file size: {Size} MB", fileInfo.Length / 1024.0 / 1024.0);

        // Clear existing data
        _logger.LogInformation("Clearing existing data...");
        _context.Risorse.RemoveRange(_context.Risorse);
        _context.Voci.RemoveRange(_context.Voci);
        await _context.SaveChangesAsync();

        // Disable change tracking for better performance
        _context.ChangeTracker.AutoDetectChangesEnabled = false;

        var settings = new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        using var reader = XmlReader.Create(_xmlFilePath, settings);
        var vociList = new List<Voce>();
        var batchSize = 100;
        var totalProcessed = 0;
        var totalSkipped = 0;
        Voce? currentVoce = null;
        var currentElementPath = new Stack<string>();

        _logger.LogInformation("Starting XML parsing with batch size: {BatchSize}", batchSize);

        while (await reader.ReadAsync())
        {
            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    var elementName = reader.Name;
                    currentElementPath.Push(elementName);

                    switch (elementName)
                    {
                        case "voci":
                            // Solo se siamo al livello delle singole voci (depth >= 2)
                            if (reader.Depth >= 2)
                            {
                                currentVoce = new Voce();
                                _logger.LogDebug("Started parsing voce at depth {Depth}", reader.Depth);
                            }
                            break;

                        case "autore":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.Autore = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop(); // ReadElementContentAsString consuma il tag
                            }
                            break;

                        case "anno":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                var anno = await reader.ReadElementContentAsStringAsync();
                                currentVoce.Anno = anno.Length == 4 && anno.StartsWith("20")
                                    ? anno.Substring(2)
                                    : anno;
                                currentElementPath.Pop();
                            }
                            break;

                        case "edizione":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.Edizione = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;

                        case "dettaglio_voce":
                            if (currentVoce != null && reader.HasAttributes)
                            {
                                currentVoce.CodiceVoce = reader.GetAttribute("codice_voce") ?? string.Empty;
                                currentVoce.PrezzoVoce = ParseDecimal(reader.GetAttribute("prezzo_voce") ?? string.Empty);
                                currentVoce.UnitaMisuraVoce = reader.GetAttribute("unita_misura_voce") ?? string.Empty;
                                currentVoce.ImportoSenzaSguiVoce = ParseDecimal(reader.GetAttribute("importo_senza_sgui_voce") ?? string.Empty);
                                currentVoce.RapportoRUVoce = ParseDecimal(reader.GetAttribute("rapporto_RU_voce") ?? string.Empty);
                                currentVoce.TipologiaRisorsa = reader.GetAttribute("tipologia_risorsa") ?? string.Empty;
                            }
                            break;

                        case "dettaglio_risorsa":
                            if (currentVoce != null)
                            {
                                var risorsa = await ParseRisorsaAsync(reader);
                                if (risorsa != null)
                                {
                                    currentVoce.Risorse.Add(risorsa);
                                }
                                currentElementPath.Pop();
                            }
                            break;

                        case "declaratoria_voce":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.DeclaratoriaVoce = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;

                        case "declaratoria_voce_dettaglio":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.DeclaratoriaVoceDettaglio = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;

                        // Livelli gerarchici
                        case "cod_liv_1":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.CodLiv1 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "descr_liv_1":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.DescrLiv1 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "cod_liv_2":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.CodLiv2 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "descr_liv_2":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.DescrLiv2 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "cod_liv_3":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.CodLiv3 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "descr_liv_3":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.DescrLiv3 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "cod_liv_4":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.CodLiv4 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "descr_liv_4":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.DescrLiv4 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "cod_liv_5":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.CodLiv5 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "descr_liv_5":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.DescrLiv5 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "cod_liv_6":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.CodLiv6 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "descr_liv_6":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.DescrLiv6 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "cod_liv_7":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.CodLiv7 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "descr_liv_7":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.DescrLiv7 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "cod_liv_8":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.CodLiv8 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "descr_liv_8":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.DescrLiv8 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "cod_liv_9":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.CodLiv9 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "descr_liv_9":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.DescrLiv9 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "cod_liv_10":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.CodLiv10 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "descr_liv_10":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.DescrLiv10 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "cod_liv_11":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.CodLiv11 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                        case "descr_liv_11":
                            if (currentVoce != null && !reader.IsEmptyElement)
                            {
                                currentVoce.DescrLiv11 = await reader.ReadElementContentAsStringAsync();
                                currentElementPath.Pop();
                            }
                            break;
                    }
                    break;

                case XmlNodeType.EndElement:
                    if (currentElementPath.Count > 0 && currentElementPath.Peek() == reader.Name)
                    {
                        currentElementPath.Pop();
                    }

                    // Quando finisce una singola <voci> al depth >= 2
                    if (reader.Name == "voci" && reader.Depth >= 2 && currentVoce != null)
                    {
                        try
                        {
                            vociList.Add(currentVoce);
                            currentVoce = null;

                            if (vociList.Count >= batchSize)
                            {
                                using var transaction = await _context.Database.BeginTransactionAsync();
                                try
                                {
                                    _context.Voci.AddRange(vociList);
                                    await _context.SaveChangesAsync();
                                    await transaction.CommitAsync();

                                    totalProcessed += vociList.Count;
                                    _logger.LogInformation("Processed {Total} voci total (skipped: {Skipped})",
                                        totalProcessed, totalSkipped);
                                    vociList.Clear();
                                    _context.ChangeTracker.Clear();
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error saving batch at {Position}", totalProcessed);
                                    await transaction.RollbackAsync();
                                    throw;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            totalSkipped++;
                            _logger.LogWarning(ex, "Skipped voce at position ~{Position}", totalProcessed + vociList.Count + totalSkipped);
                            currentVoce = null;
                        }
                    }
                    break;
            }
        }

        // Save remaining voci
        if (vociList.Any())
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Voci.AddRange(vociList);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                totalProcessed += vociList.Count;
                _logger.LogInformation("Processed final {Count} voci - Total: {Total} (skipped: {Skipped})",
                    vociList.Count, totalProcessed, totalSkipped);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving final batch");
                await transaction.RollbackAsync();
                throw;
            }
        }

        // Re-enable change tracking
        _context.ChangeTracker.AutoDetectChangesEnabled = true;

        stopwatch.Stop();

        // Log final statistics
        var vociCount = await _context.Voci.CountAsync();
        var risorseCount = await _context.Risorse.CountAsync();

        _logger.LogInformation("=== Database initialization completed ===");
        _logger.LogInformation("Total voci loaded: {VociCount}", vociCount);
        _logger.LogInformation("Total risorse loaded: {RisorseCount}", risorseCount);
        _logger.LogInformation("Elapsed:  {Elapsed}", stopwatch.Elapsed);
    }
    //private async Task<Voce?> ParseVociNodeAsync(XmlReader reader)
    //{
    //    var voce = new Voce();

    //    try
    //    {
    //        var depth = reader.Depth;

    //        while (await reader.ReadAsync())
    //        {
    //            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "voci" && reader.Depth == depth)
    //            {
    //                break;
    //            }

    //            if (reader.NodeType == XmlNodeType.Element)
    //            {
    //                var elementName = reader.Name;

    //                switch (elementName)
    //                {
    //                    case "riferimenti_voce":
    //                        await ParseRiferimentiVoceAsync(reader, voce);
    //                        break;

    //                    case "dettaglio_voce":
    //                        await ParseDettaglioVoceAsync(reader, voce);
    //                        break;
    //                }
    //            }
    //        }

    //        return voce;
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "Error parsing voci node");
    //        return null;
    //    }
    //}

    //private async Task ParseRiferimentiVoceAsync(XmlReader reader, Voce voce)
    //{
    //    var depth = reader.Depth;

    //    while (await reader.ReadAsync())
    //    {
    //        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "riferimenti_voce" && reader.Depth == depth)
    //        {
    //            break;
    //        }

    //        if (reader.NodeType == XmlNodeType.Element)
    //        {
    //            var elementName = reader.Name;

    //            if (reader.IsEmptyElement)
    //                continue;

    //            try
    //            {
    //                var value = await reader.ReadElementContentAsStringAsync();

    //                switch (elementName)
    //                {
    //                    case "autore":
    //                        voce.Autore = value;
    //                        break;
    //                    case "anno":
    //                        // Rimuovi "20" se l'anno è 2025 -> diventa "25"
    //                        voce.Anno = value.Length == 4 && value.StartsWith("20")
    //                            ? value.Substring(2)
    //                            : value;
    //                        break;
    //                    case "edizione":
    //                        voce.Edizione = value;
    //                        break;
    //                }
    //            }
    //            catch (XmlException)
    //            {
    //                // Skip elementi con figli
    //                continue;
    //            }
    //        }
    //    }
    //}

    //private async Task ParseDettaglioVoceAsync(XmlReader reader, Voce voce)
    //{
    //    // Leggi gli attributi
    //    if (reader.HasAttributes)
    //    {
    //        voce.CodiceVoce = reader.GetAttribute("codice_voce") ?? string.Empty;
    //        voce.PrezzoVoce = ParseDecimal(reader.GetAttribute("prezzo_voce") ?? string.Empty);
    //        voce.UnitaMisuraVoce = reader.GetAttribute("unita_misura_voce") ?? string.Empty;
    //        voce.ImportoSenzaSguiVoce = ParseDecimal(reader.GetAttribute("importo_senza_sgui_voce") ?? string.Empty);
    //        voce.RapportoRUVoce = ParseDecimal(reader.GetAttribute("rapporto_RU_voce") ?? string.Empty);
    //        voce.TipologiaRisorsa = reader.GetAttribute("tipologia_risorsa") ?? string.Empty;
    //    }

    //    var depth = reader.Depth;

    //    while (await reader.ReadAsync())
    //    {
    //        if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "dettaglio_voce" && reader.Depth == depth)
    //        {
    //            break;
    //        }

    //        if (reader.NodeType == XmlNodeType.Element)
    //        {
    //            var elementName = reader.Name;

    //            // Gestisci dettaglio_risorsa
    //            if (elementName == "dettaglio_risorsa")
    //            {
    //                var risorsa = await ParseRisorsaAsync(reader);
    //                if (risorsa != null)
    //                {
    //                    voce.Risorse.Add(risorsa);
    //                }
    //                continue;
    //            }

    //            if (reader.IsEmptyElement)
    //                continue;

    //            try
    //            {
    //                var value = await reader.ReadElementContentAsStringAsync();

    //                switch (elementName)
    //                {
    //                    case "declaratoria_voce": voce.DeclaratoriaVoce = value; break;
    //                    case "declaratoria_voce_dettaglio": voce.DeclaratoriaVoceDettaglio = value; break;
    //                    case "cod_liv_1": voce.CodLiv1 = value; break;
    //                    case "descr_liv_1": voce.DescrLiv1 = value; break;
    //                    case "cod_liv_2": voce.CodLiv2 = value; break;
    //                    case "descr_liv_2": voce.DescrLiv2 = value; break;
    //                    case "cod_liv_3": voce.CodLiv3 = value; break;
    //                    case "descr_liv_3": voce.DescrLiv3 = value; break;
    //                    case "cod_liv_4": voce.CodLiv4 = value; break;
    //                    case "descr_liv_4": voce.DescrLiv4 = value; break;
    //                    case "cod_liv_5": voce.CodLiv5 = value; break;
    //                    case "descr_liv_5": voce.DescrLiv5 = value; break;
    //                    case "cod_liv_6": voce.CodLiv6 = value; break;
    //                    case "descr_liv_6": voce.DescrLiv6 = value; break;
    //                    case "cod_liv_7": voce.CodLiv7 = value; break;
    //                    case "descr_liv_7": voce.DescrLiv7 = value; break;
    //                    case "cod_liv_8": voce.CodLiv8 = value; break;
    //                    case "descr_liv_8": voce.DescrLiv8 = value; break;
    //                    case "cod_liv_9": voce.CodLiv9 = value; break;
    //                    case "descr_liv_9": voce.DescrLiv9 = value; break;
    //                    case "cod_liv_10": voce.CodLiv10 = value; break;
    //                    case "descr_liv_10": voce.DescrLiv10 = value; break;
    //                    case "cod_liv_11": voce.CodLiv11 = value; break;
    //                    case "descr_liv_11": voce.DescrLiv11 = value; break;
    //                }
    //            }
    //            catch (XmlException)
    //            {
    //                // Skip elementi con figli
    //                continue;
    //            }
    //        }
    //    }
    //}
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
