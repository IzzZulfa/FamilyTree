using family_tree.Data;
using family_tree.Infrastructure;
using family_tree.Seed;
using family_tree.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => JsonSettings.Configure(options.JsonSerializerOptions));
// The OpenAPI generator reads these (minimal-API) JSON options rather than the MVC ones above,
// so apply the same settings here for the document to describe enums as strings.
builder.Services.ConfigureHttpJsonOptions(options => JsonSettings.Configure(options.SerializerOptions));
builder.Services.AddOpenApi();

// Turns ValidationException/NotFoundException from services into 400/404 ProblemDetails.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

// The connection string is read when the DbContext is first resolved (not at startup),
// so the test project can override "ConnectionStrings:FamilyTree" to point at a test database.
builder.Services.AddDbContext<FamilyTreeDbContext>((services, options) =>
{
    var connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString("FamilyTree")
        ?? throw new InvalidOperationException("Connection string 'FamilyTree' is not configured.");
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
});

builder.Services.AddScoped<TreeService>();
builder.Services.AddScoped<PersonService>();
builder.Services.AddScoped<ParentChildService>();
builder.Services.AddScoped<PartnershipService>();
builder.Services.AddScoped<SeedLoader>();

var app = builder.Build();

// `dotnet run --project family_tree -- --seed seed/hartley-family.json` loads the file and exits
// instead of starting the web server.
if (args is ["--seed", var seedPath])
{
    Environment.ExitCode = await SeedCommand.RunAsync(app.Services, seedPath);
    return;
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // AddOpenApi/MapOpenApi generate the document at /openapi/v1.json;
    // Swagger UI is just a browser front-end for it, served at /swagger.
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Family Tree API v1"));
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

// Top-level statements generate an internal Program class; making it public lets the test
// project's WebApplicationFactory<Program> reference it. (.NET 10 does this automatically.)
public partial class Program;
