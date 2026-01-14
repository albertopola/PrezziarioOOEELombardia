using System.Net.Http.Json;
using PrezziarioOOEELombardia.Shared;

namespace PrezziarioOOEELombardia.Client.Services;

public class PrezziarioApiClient
{
    private readonly HttpClient _httpClient;

    public PrezziarioApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<TreeNodeDTO>?> GetTreeRootAsync()
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<TreeNodeDTO>>("api/prezziario/tree");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting tree root: {ex.Message}");
            return null;
        }
    }

    public async Task<List<TreeNodeDTO>?> GetTreeChildrenAsync(int level, string code)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<List<TreeNodeDTO>>($"api/prezziario/tree/{level}/{code}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting tree children: {ex.Message}");
            return null;
        }
    }

    public async Task<SearchResultDTO?> SearchAsync(SearchRequestDTO request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/prezziario/search", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SearchResultDTO>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error performing search: {ex.Message}");
            return null;
        }
    }

    public async Task<VoceDTO?> GetVoceDetailAsync(string codiceVoce)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<VoceDTO>($"api/prezziario/voce/{codiceVoce}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting voce detail: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> InitializeDatabaseAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/prezziario/initialize");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing database: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> GetDatabaseStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<DatabaseStatusResponse>("api/prezziario/status");
            return response?.IsInitialized ?? false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking database status: {ex.Message}");
            return false;
        }
    }

    private class DatabaseStatusResponse
    {
        public bool IsInitialized { get; set; }
    }
}
