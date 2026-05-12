using Microsoft.EntityFrameworkCore;
using Serilog;
using WcmsImport.Api.Data;
using WcmsImport.Api.Notifications;
using WcmsImport.Api.Repositories;
using WcmsImport.Api.Services;

//Serilog structured logging                        
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

//Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "WCMS Import API",
        Version = "v1",
        Description = "Imports content from external WCMS platforms and notifies upstream systems."
    });
});

//Database
builder.Services.AddDbContext<WcmsDbContext>(options =>
    options.UseInMemoryDatabase("WcmsImportDb"));

builder.Services.AddHttpClient<IUpstreamNotifier, UpstreamNotifier>();

builder.Services.AddScoped<IContentRepository, ContentRepository>();
builder.Services.AddScoped<IImportService, ImportService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WCMS Import API v1");
    c.RoutePrefix = string.Empty;
});

app.UseSerilogRequestLogging(); 
app.UseAuthorization();
app.MapControllers();

Log.Information("WCMS Import API starting up...");
app.Run();

public partial class Program { }
