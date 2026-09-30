using System.Data.Common;
using System.Text.Json;

namespace family_tree.Seed;

/// <summary>The console side of seeding: reads the file, runs the loader, prints the outcome.</summary>
public static class SeedCommand
{
    /// <returns>The process exit code: 0 on success, 1 on failure.</returns>
    public static async Task<int> RunAsync(IServiceProvider services, string path)
    {
        try
        {
            var file = await SeedFile.ReadAsync(path, CancellationToken.None);

            using var scope = services.CreateScope();
            var loader = scope.ServiceProvider.GetRequiredService<SeedLoader>();
            var result = await loader.LoadAsync(file, CancellationToken.None);

            Console.WriteLine($"Seeded tree '{file.Tree}' (id {result.TreeId}): {result.People} people, "
                + $"{result.ParentLinks} parent links, {result.Partnerships} partnerships.");
            foreach (var warning in result.Warnings)
            {
                Console.WriteLine($"  warning: {warning}");
            }
            return 0;
        }
        // DbException covers database problems such as a wrong password or Postgres not running.
        catch (Exception ex) when (ex is SeedException or JsonException or IOException or DbException)
        {
            Console.Error.WriteLine($"Seeding failed, nothing was saved: {ex.Message}");
            return 1;
        }
    }
}
