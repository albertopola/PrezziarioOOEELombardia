using Microsoft.AspNetCore.Mvc;
using PrezziarioOOEELombardia.Server.Services;
using PrezziarioOOEELombardia.Shared;

namespace PrezziarioOOEELombardia.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrezziarioController : ControllerBase
{
    private readonly SearchService _searchService;
    private readonly XmlParserService _xmlParserService;
    private readonly ILogger<PrezziarioController> _logger;

    public PrezziarioController(
        SearchService searchService,
        XmlParserService xmlParserService,
        ILogger<PrezziarioController> logger)
    {
        _searchService = searchService;
        _xmlParserService = xmlParserService;
        _logger = logger;
    }

    [HttpGet("tree")]
    public async Task<ActionResult<List<TreeNodeDTO>>> GetTreeRoot()
    {
        try
        {
            var roots = await _searchService.GetTreeRootAsync();
            return Ok(roots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tree root");
            return StatusCode(500, "An error occurred while fetching the tree root");
        }
    }

    [HttpGet("tree/{level}/{code}")]
    public async Task<ActionResult<List<TreeNodeDTO>>> GetTreeChildren(int level, string code)
    {
        try
        {
            var children = await _searchService.GetTreeChildrenAsync(level, code);
            return Ok(children);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tree children for level {Level}, code {Code}", level, code);
            return StatusCode(500, "An error occurred while fetching tree children");
        }
    }

    [HttpPost("search")]
    public async Task<ActionResult<SearchResultDTO>> Search([FromBody] SearchRequestDTO request)
    {
        try
        {
            var result = await _searchService.SearchAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing search");
            return StatusCode(500, "An error occurred while performing the search");
        }
    }

    [HttpGet("voce/{codiceVoce}")]
    public async Task<ActionResult<VoceDTO>> GetVoceDetail(string codiceVoce)
    {
        try
        {
            var voce = await _searchService.GetVoceByCodeAsync(codiceVoce);
            if (voce == null)
            {
                return NotFound($"Voce with code {codiceVoce} not found");
            }
            return Ok(voce);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting voce detail for {CodiceVoce}", codiceVoce);
            return StatusCode(500, "An error occurred while fetching voce details");
        }
    }

    [HttpGet("initialize")]
    public async Task<ActionResult> InitializeDatabase()
    {
        try
        {
            var isInitialized = await _xmlParserService.IsDatabaseInitializedAsync();
            if (isInitialized)
            {
                return Ok(new { message = "Database is already initialized", isInitialized = true });
            }

            await _xmlParserService.InitializeDatabaseAsync();
            return Ok(new { message = "Database initialized successfully", isInitialized = true });
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "XML file not found during initialization");
            return NotFound(new { message = ex.Message, isInitialized = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database");
            return StatusCode(500, new { message = "An error occurred while initializing the database", isInitialized = false });
        }
    }

    [HttpGet("status")]
    public async Task<ActionResult> GetStatus()
    {
        try
        {
            var isInitialized = await _xmlParserService.IsDatabaseInitializedAsync();
            return Ok(new { isInitialized });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking database status");
            return StatusCode(500, "An error occurred while checking database status");
        }
    }
}
