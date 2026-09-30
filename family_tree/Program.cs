using System.Text.Json;
using System.Text.Json.Serialization;
using family_tree.Data;
using family_tree.Infrastructure;
using family_tree.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));
// The OpenAPI generator reads these (minimal-API) JSON options rather than the MVC ones above,
// so apply the same settings here for the document to describe enums as strings.
builder.Services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));
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

var app = builder.Build();

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

static void ConfigureJson(JsonSerializerOptions json)
{
    // Enums travel as names ("Biological"), and numbers like 99 are rejected.
    json.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
    // Constructor parameters without a default value must be present in the JSON,
    // and non-nullable properties can't be null. Violations become 400 responses.
    json.RespectRequiredConstructorParameters = true;
    json.RespectNullableAnnotations = true;
}
