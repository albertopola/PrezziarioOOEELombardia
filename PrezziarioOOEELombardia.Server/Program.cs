using Microsoft.EntityFrameworkCore;
using PrezziarioOOEELombardia.Server.Data;
using PrezziarioOOEELombardia.Server.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure SQLite database
var connectionString = builder.Configuration.GetConnectionString("PrezziarioDb") 
    ?? "Data Source=prezziario.db";
builder.Services.AddDbContext<PrezziarioDbContext>(options =>
    options.UseSqlite(connectionString));

// Add custom services
builder.Services.AddScoped<XmlParserService>();
builder.Services.AddScoped<SearchService>();

// Configure CORS for Blazor WebAssembly
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorWasmCors", policy =>
    {
        policy.WithOrigins("https://localhost:5001", "http://localhost:5000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add response compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PrezziarioDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseCors("BlazorWasmCors");
app.UseAuthorization();
app.MapControllers();

app.Run();
