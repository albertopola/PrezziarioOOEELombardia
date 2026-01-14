using Microsoft.EntityFrameworkCore;
using PrezziarioOOEELombardia.Server.Data;
using PrezziarioOOEELombardia.Shared;

namespace PrezziarioOOEELombardia.Server.Services;

public class SearchService
{
    private readonly PrezziarioDbContext _context;
    private readonly ILogger<SearchService> _logger;

    public SearchService(PrezziarioDbContext context, ILogger<SearchService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SearchResultDTO> SearchAsync(SearchRequestDTO request)
    {
        var query = _context.Voci.Include(v => v.Risorse).AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim();

            if (request.SearchFromEnd)
            {
                // Ricerca dagli ultimi livelli del codice
                query = query.Where(v =>
                    v.CodiceVoce.Contains(searchTerm) ||
                    (v.CodLiv11 != null && v.CodLiv11.Contains(searchTerm)) ||
                    (v.CodLiv10 != null && v.CodLiv10.Contains(searchTerm)) ||
                    (v.CodLiv9 != null && v.CodLiv9.Contains(searchTerm)) ||
                    (v.CodLiv8 != null && v.CodLiv8.Contains(searchTerm)));
            }
            else
            {
                // Ricerca standard: codice voce o declaratoria
                query = query.Where(v =>
                    v.CodiceVoce.Contains(searchTerm) ||
                    v.DeclaratoriaVoce.Contains(searchTerm) ||
                    v.DeclaratoriaVoceDettaglio.Contains(searchTerm));
            }
        }

        if (request.Level.HasValue)
        {
            query = ApplyLevelFilter(query, request.Level.Value);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var voci = await query
            .OrderBy(v => v.CodiceVoce)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new SearchResultDTO
        {
            Results = voci.Select(MapToDTO).ToList(),
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            TotalPages = totalPages
        };
    }

    public async Task<VoceDTO?> GetVoceByCodeAsync(string codiceVoce)
    {
        var voce = await _context.Voci
            .Include(v => v.Risorse)
            .FirstOrDefaultAsync(v => v.CodiceVoce == codiceVoce);

        return voce != null ? MapToDTO(voce) : null;
    }
    public async Task<List<TreeNodeDTO>> GetTreeRootAsync()
    {
        // Primo livello: Autore
        var autori = await _context.Voci
            .Where(v => !string.IsNullOrEmpty(v.Autore))
            .Select(v => v.Autore)
            .Distinct()
            .ToListAsync();

        return autori.Select(autore => new TreeNodeDTO
        {
            Code = autore,
            Description = autore,
            Level = 0,
            HasChildren = true
        }).OrderBy(n => n.Code).ToList();
    }

    public async Task<List<TreeNodeDTO>> GetTreeChildrenAsync(int level, string code)
    {
        _logger.LogWarning($"GetTreeChildrenAsync: level={level}, code={code}");

        // Split la chiave pipe, es: "LOM|25|2|RM|87|10|15|Za001"
        var keys = code.Split('|');
        string autore = keys.Length > 0 ? keys[0] : "";
        string anno = keys.Length > 1 ? keys[1] : "";
        string edizione = keys.Length > 2 ? keys[2] : "";

        // Mappa livelli database
        string[] codLivNames = new[] {
        "CodLiv1", "CodLiv2", "CodLiv3", "CodLiv4", "CodLiv5",
        "CodLiv6", "CodLiv7", "CodLiv8", "CodLiv9", "CodLiv10", "CodLiv11"
    };
        string[] descrLivNames = new[] {
        "DescrLiv1", "DescrLiv2", "DescrLiv3", "DescrLiv4", "DescrLiv5",
        "DescrLiv6", "DescrLiv7", "DescrLiv8", "DescrLiv9", "DescrLiv10", "DescrLiv11"
    };

        // Level:
        // 0: Autore --> restituisci Anni
        if (level == 0)
        {
            var anni = await _context.Voci
                .Where(v => v.Autore == autore && (!string.IsNullOrEmpty(v.Anno)))
                .Select(v => v.Anno)
                .Distinct()
                .ToListAsync();
            _logger.LogWarning($"ANNI trovati per autore={autore}: {string.Join(",", anni)}");

            return anni.Select(a => new TreeNodeDTO
            {
                Code = $"{autore}|{a}",
                Description = "20" + a,
                Level = 1,
                HasChildren = true
            }).OrderBy(n => n.Code).ToList();
        }

        // 1: Anno --> Edizioni
        if (level == 1)
        {
            var edizioni = await _context.Voci
                .Where(v => v.Autore == autore && v.Anno == anno && !string.IsNullOrEmpty(v.Edizione))
                .Select(v => v.Edizione)
                .Distinct()
                .ToListAsync();

            return edizioni.Select(e => new TreeNodeDTO
            {
                Code = $"{autore}|{anno}|{e}",
                Description = "Edizione " + e,
                Level = 2,
                HasChildren = true
            }).OrderBy(n => n.Code).ToList();
        }

        // 2: Edizione --> Livello 1
        if (level == 2)
        {
            var liv1 = await _context.Voci
                .Where(v => v.Autore == autore && v.Anno == anno && v.Edizione == edizione && v.CodLiv1 != null)
                .GroupBy(v => new { v.CodLiv1, v.DescrLiv1 })
                .Select(g => new TreeNodeDTO
                {
                    Code = $"{autore}|{anno}|{edizione}|{g.Key.CodLiv1}",
                    Description = g.Key.DescrLiv1 ?? "",
                    Level = 3,
                    HasChildren = true
                })
                .OrderBy(n => n.Code)
                .ToListAsync();

            return liv1;
        }

        // Dal livello 3 in poi, mappiamo ai livelli CodLivX (X: 2..11)
        if (level >= 3 && level < 13)
        {
            // Determine quanti livelli di CodLiv sono passati nella chiave
            int currentLivIndex = level - 3; // 3 -> CodLiv2, 4 -> CodLiv3, ..., 13 -> CodLiv12 (non esiste, gestito dopo)
            if (currentLivIndex >= 0 && currentLivIndex < codLivNames.Length)
            {
                // Prepara i valori precedenti per la WHERE
                var query = _context.Voci.AsQueryable();

                query = query.Where(v =>
                    v.Autore == autore &&
                    v.Anno == anno &&
                    v.Edizione == edizione);

                // Applica tutti i CodLiv precedenti
                for (int i = 0; i < currentLivIndex; i++)
                {
                    var codValue = keys.Length > 3 + i ? keys[3 + i] : null;
                    string codLiv = codLivNames[i];
                    if (!string.IsNullOrEmpty(codValue))
                    {
                        query = query.Where(v => EF.Property<string>(v, codLiv) == codValue);
                    }
                }

                string codLivCurrent = codLivNames[currentLivIndex];
                string descrLivCurrent = descrLivNames[currentLivIndex];

                // Lista prossimi codici disponibili a questo livello
                var gruppi = await query
                    .Where(v => EF.Property<string>(v, codLivCurrent) != null)
                    .GroupBy(v => new
                    {
                        Cod = EF.Property<string>(v, codLivCurrent),
                        Descr = EF.Property<string>(v, descrLivCurrent)
                    })
                    .Select(g => new TreeNodeDTO
                    {
                        Code = code + "|" + g.Key.Cod,
                        Description = g.Key.Descr ?? "",
                        Level = level + 1,
                        HasChildren = true
                    })
                    .OrderBy(n => n.Code)
                    .ToListAsync();

                // Se non ci sono figli, mostra direttamente le voci (foglie)
                if (!gruppi.Any())
                {
                    var voci = await query
                        .Where(v =>
                            // serve che i codici precedenti matchino!
                            (EF.Property<string>(v, codLivCurrent) == null) &&
                            !string.IsNullOrEmpty(v.CodiceVoce)
                        )
                        .Select(v => new TreeNodeDTO
                        {
                            Code = v.CodiceVoce,
                            Description = v.DeclaratoriaVoce,
                            Level = 12,
                            HasChildren = false
                        })
                        .OrderBy(n => n.Code)
                        .ToListAsync();

                    return voci;
                }

                return gruppi;
            }
        }

        // fallback vuoto
        return new List<TreeNodeDTO>();
    }

    private IQueryable<Voce> ApplyLevelFilter(IQueryable<Voce> query, int level)
    {
        return level switch
        {
            1 => query.Where(v => v.CodLiv1 != null),
            2 => query.Where(v => v.CodLiv2 != null),
            3 => query.Where(v => v.CodLiv3 != null),
            4 => query.Where(v => v.CodLiv4 != null),
            5 => query.Where(v => v.CodLiv5 != null),
            6 => query.Where(v => v.CodLiv6 != null),
            7 => query.Where(v => v.CodLiv7 != null),
            8 => query.Where(v => v.CodLiv8 != null),
            9 => query.Where(v => v.CodLiv9 != null),
            10 => query.Where(v => v.CodLiv10 != null),
            11 => query.Where(v => v.CodLiv11 != null),
            _ => query
        };
    }

    private static VoceDTO MapToDTO(Voce voce)
    {
        return new VoceDTO
        {
            // Nuovi campi
            Autore = voce.Autore,
            Anno = voce.Anno,
            Edizione = voce.Edizione,

            // Campi già presenti
            CodiceVoce = voce.CodiceVoce,
            PrezzoVoce = voce.PrezzoVoce,
            UnitaMisuraVoce = voce.UnitaMisuraVoce,
            ImportoSenzaSguiVoce = voce.ImportoSenzaSguiVoce,
            RapportoRUVoce = voce.RapportoRUVoce,
            TipologiaRisorsa = voce.TipologiaRisorsa,
            DeclaratoriaVoce = voce.DeclaratoriaVoce,
            DeclaratoriaVoceDettaglio = voce.DeclaratoriaVoceDettaglio,
            CodLiv1 = voce.CodLiv1,
            DescrLiv1 = voce.DescrLiv1,
            CodLiv2 = voce.CodLiv2,
            DescrLiv2 = voce.DescrLiv2,
            CodLiv3 = voce.CodLiv3,
            DescrLiv3 = voce.DescrLiv3,
            CodLiv4 = voce.CodLiv4,
            DescrLiv4 = voce.DescrLiv4,
            CodLiv5 = voce.CodLiv5,
            DescrLiv5 = voce.DescrLiv5,
            CodLiv6 = voce.CodLiv6,
            DescrLiv6 = voce.DescrLiv6,
            CodLiv7 = voce.CodLiv7,
            DescrLiv7 = voce.DescrLiv7,
            CodLiv8 = voce.CodLiv8,
            DescrLiv8 = voce.DescrLiv8,
            CodLiv9 = voce.CodLiv9,
            DescrLiv9 = voce.DescrLiv9,
            CodLiv10 = voce.CodLiv10,
            DescrLiv10 = voce.DescrLiv10,
            CodLiv11 = voce.CodLiv11,
            DescrLiv11 = voce.DescrLiv11,
            Risorse = voce.Risorse.Select(r => new RisorsaDTO
            {
                CodificaRisorsa = r.CodificaRisorsa,
                UdmRisorsa = r.UdmRisorsa,
                QuantitaRisorsa = r.QuantitaRisorsa,
                PrezzoRisorsa = r.PrezzoRisorsa,
                ImportoRisorsa = r.ImportoRisorsa,
                TipologiaRisorsa = r.TipologiaRisorsa,
                DeclaratoriaRisorsa = r.DeclaratoriaRisorsa
            }).ToList()
        };
    }
}
