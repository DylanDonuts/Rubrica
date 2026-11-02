using Microsoft.AspNetCore.Routing;
using Npgsql;
using Rubrica.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(services =>
{
    var dataSourceBuilder = new NpgsqlDataSourceBuilder(DatabaseConfiguration.GetConnectionString(builder.Configuration));
    dataSourceBuilder.UseLoggerFactory(services.GetRequiredService<ILoggerFactory>());
    return dataSourceBuilder.Build();
});
builder.Services.AddScoped<ContactRepository>();
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5174"];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

//var app = builder.Build();
WebApplication app = builder.Build();

app.UseExceptionHandler();
app.UseCors();
app.MapContactEndpoints();
app.MapGet("/health", async (NpgsqlDataSource dataSource, CancellationToken cancellationToken) =>
{
    await using var command = dataSource.CreateCommand("SELECT NOW()");
    var databaseTime = await command.ExecuteScalarAsync(cancellationToken);
    return Results.Ok(new { status = "ok", databaseTime });
});
app.MapFallback(() => Results.NotFound(new ApiError("Rotta non trovata")));
app.Run();

public partial class Program;
