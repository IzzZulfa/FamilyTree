using family_tree.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// The connection string is read when the DbContext is first resolved (not at startup),
// so the test project can override "ConnectionStrings:FamilyTree" to point at a test database.
builder.Services.AddDbContext<FamilyTreeDbContext>((services, options) =>
{
    var connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString("FamilyTree")
        ?? throw new InvalidOperationException("Connection string 'FamilyTree' is not configured.");
    options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
