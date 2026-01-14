using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PrezziarioOOEELombardia.Client;
using PrezziarioOOEELombardia.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for API calls
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7151/";
builder.Services.AddScoped(sp => new HttpClient 
{ 
    BaseAddress = new Uri(apiBaseUrl),
    Timeout = TimeSpan.FromMinutes(30) // Aumentato a 30 minuti 
});

// Add custom services
builder.Services.AddScoped<PrezziarioApiClient>();

await builder.Build().RunAsync();
