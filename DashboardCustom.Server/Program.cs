using DashboardCustom.Server.Service;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:5206");

// Add services to the container.
builder.Logging.ClearProviders();
builder.Logging.AddProvider(new FileLoggerProvider());
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<AudioService>();
builder.Services.AddSingleton<LightService>();
builder.Services.AddSingleton<DeviceService>();
builder.Services.AddSingleton<SystemService>();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseDefaultFiles();
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
