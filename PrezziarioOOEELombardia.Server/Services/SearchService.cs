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
        var roots = await _context.Voci
            .Where(v => v.CodLiv1 != null)
            .Select(v => new { v.CodLiv1, v.DescrLiv1 })
            .Distinct()
            .ToListAsync();

        return roots.Select(r => new TreeNodeDTO
        {
            Code = r.CodLiv1!,
            Description = r.DescrLiv1 ?? "",
            Level = 1,
            HasChildren = true
        }).OrderBy(n => n.Code).ToList();
    }

    public async Task<List<TreeNodeDTO>> GetTreeChildrenAsync(int level, string code)
    {
        if (level < 1 || level > 11)
            return new List<TreeNodeDTO>();

        List<TreeNodeDTO> nodes;

        switch (level)
        {
            case 1:
                var level2 = await _context.Voci
                    .Where(v => v.CodLiv1 == code && v.CodLiv2 != null)
                    .Select(v => new { v.CodLiv2, v.DescrLiv2 })
                    .Distinct()
                    .ToListAsync();
                nodes = level2.Select(n => new TreeNodeDTO
                {
                    Code = n.CodLiv2!,
                    Description = n.DescrLiv2 ?? "",
                    Level = 2,
                    HasChildren = true
                }).ToList();
                break;

            case 2:
                var level3 = await _context.Voci
                    .Where(v => v.CodLiv2 == code && v.CodLiv3 != null)
                    .Select(v => new { v.CodLiv3, v.DescrLiv3 })
                    .Distinct()
                    .ToListAsync();
                nodes = level3.Select(n => new TreeNodeDTO
                {
                    Code = n.CodLiv3!,
                    Description = n.DescrLiv3 ?? "",
                    Level = 3,
                    HasChildren = true
                }).ToList();
                break;

            case 3:
                var level4 = await _context.Voci
                    .Where(v => v.CodLiv3 == code && v.CodLiv4 != null)
                    .Select(v => new { v.CodLiv4, v.DescrLiv4 })
                    .Distinct()
                    .ToListAsync();
                nodes = level4.Select(n => new TreeNodeDTO
                {
                    Code = n.CodLiv4!,
                    Description = n.DescrLiv4 ?? "",
                    Level = 4,
                    HasChildren = true
                }).ToList();
                break;

            case 4:
                var level5 = await _context.Voci
                    .Where(v => v.CodLiv4 == code && v.CodLiv5 != null)
                    .Select(v => new { v.CodLiv5, v.DescrLiv5 })
                    .Distinct()
                    .ToListAsync();
                nodes = level5.Select(n => new TreeNodeDTO
                {
                    Code = n.CodLiv5!,
                    Description = n.DescrLiv5 ?? "",
                    Level = 5,
                    HasChildren = true
                }).ToList();
                break;

            case 5:
                var level6 = await _context.Voci
                    .Where(v => v.CodLiv5 == code && v.CodLiv6 != null)
                    .Select(v => new { v.CodLiv6, v.DescrLiv6 })
                    .Distinct()
                    .ToListAsync();
                nodes = level6.Select(n => new TreeNodeDTO
                {
                    Code = n.CodLiv6!,
                    Description = n.DescrLiv6 ?? "",
                    Level = 6,
                    HasChildren = true
                }).ToList();
                break;

            case 6:
                var level7 = await _context.Voci
                    .Where(v => v.CodLiv6 == code && v.CodLiv7 != null)
                    .Select(v => new { v.CodLiv7, v.DescrLiv7 })
                    .Distinct()
                    .ToListAsync();
                nodes = level7.Select(n => new TreeNodeDTO
                {
                    Code = n.CodLiv7!,
                    Description = n.DescrLiv7 ?? "",
                    Level = 7,
                    HasChildren = true
                }).ToList();
                break;

            case 7:
                var level8 = await _context.Voci
                    .Where(v => v.CodLiv7 == code && v.CodLiv8 != null)
                    .Select(v => new { v.CodLiv8, v.DescrLiv8 })
                    .Distinct()
                    .ToListAsync();
                nodes = level8.Select(n => new TreeNodeDTO
                {
                    Code = n.CodLiv8!,
                    Description = n.DescrLiv8 ?? "",
                    Level = 8,
                    HasChildren = true
                }).ToList();
                break;

            case 8:
                var level9 = await _context.Voci
                    .Where(v => v.CodLiv8 == code && v.CodLiv9 != null)
                    .Select(v => new { v.CodLiv9, v.DescrLiv9 })
                    .Distinct()
                    .ToListAsync();
                nodes = level9.Select(n => new TreeNodeDTO
                {
                    Code = n.CodLiv9!,
                    Description = n.DescrLiv9 ?? "",
                    Level = 9,
                    HasChildren = true
                }).ToList();
                break;

            case 9:
                var level10 = await _context.Voci
                    .Where(v => v.CodLiv9 == code && v.CodLiv10 != null)
                    .Select(v => new { v.CodLiv10, v.DescrLiv10 })
                    .Distinct()
                    .ToListAsync();
                nodes = level10.Select(n => new TreeNodeDTO
                {
                    Code = n.CodLiv10!,
                    Description = n.DescrLiv10 ?? "",
                    Level = 10,
                    HasChildren = true
                }).ToList();
                break;

            case 10:
                var level11 = await _context.Voci
                    .Where(v => v.CodLiv10 == code && v.CodLiv11 != null)
                    .Select(v => new { v.CodLiv11, v.DescrLiv11 })
                    .Distinct()
                    .ToListAsync();
                nodes = level11.Select(n => new TreeNodeDTO
                {
                    Code = n.CodLiv11!,
                    Description = n.DescrLiv11 ?? "",
                    Level = 11,
                    HasChildren = true
                }).ToList();
                break;

            case 11:
                var voci = await _context.Voci
                    .Where(v => v.CodLiv11 == code)
                    .Select(v => new { v.CodiceVoce, v.DeclaratoriaVoce })
                    .ToListAsync();
                nodes = voci.Select(v => new TreeNodeDTO
                {
                    Code = v.CodiceVoce,
                    Description = v.DeclaratoriaVoce,
                    Level = 12,
                    HasChildren = false
                }).ToList();
                break;

            default:
                nodes = new List<TreeNodeDTO>();
                break;
        }

        return nodes.OrderBy(n => n.Code).ToList();
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
