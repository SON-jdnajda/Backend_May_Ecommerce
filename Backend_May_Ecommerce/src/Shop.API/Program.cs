using Shop.API.Extensions;
using Shop.API.Infrastructure;
using Shop.Application;
using Shop.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CustomExceptionHandler>();

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddObservability(builder.Configuration, builder.Environment);
builder.Services.AddHealthProbes();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// Scrape and probe endpoints are mapped after MapControllers but are not
// controllers themselves - they stay unauthenticated on purpose so Prometheus
// and the orchestrator can reach them without a token. In production they
// belong on a separate, network-restricted port.
app.MapPrometheusScrapingEndpoint();
app.MapHealthProbes();

app.Run();
