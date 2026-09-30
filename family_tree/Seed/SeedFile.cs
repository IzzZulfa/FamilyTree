using System.Text.Json;
using family_tree.Dtos;
using family_tree.Entities;
using family_tree.Infrastructure;

namespace family_tree.Seed;

/// <summary>
/// The shape of a file in /seed. People get a local <see cref="SeedPerson.Key"/> so edges can
/// refer to them before database ids exist.
/// </summary>
public record SeedFile(
    string Tree,
    List<SeedPerson> People,
    List<SeedParentLink> Parents,
    List<SeedPartnership> Partnerships)
{
    private static readonly JsonSerializerOptions Json = CreateJsonOptions();

    public static async Task<SeedFile> ReadAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<SeedFile>(stream, Json, ct)
            ?? throw new SeedException($"Seed file '{path}' is empty.");
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        JsonSettings.Configure(options);
        return options;
    }
}

/// <summary>The same fields as the POST /people body, plus a key.</summary>
public record SeedPerson(
    string Key,
    string GivenName,
    string? Surname = null,
    string? BirthSurname = null,
    Sex Sex = Sex.Unknown,
    FuzzyDateDto? BirthDate = null,
    string? BirthPlace = null,
    FuzzyDateDto? DeathDate = null,
    string? Notes = null)
{
    public SavePersonRequest ToRequest() =>
        new(GivenName, Surname, BirthSurname, Sex, BirthDate, BirthPlace, DeathDate, Notes);
}

public record SeedParentLink(string Child, string Parent, ParentChildType Type);

public record SeedPartnership(
    string Person1,
    string Person2,
    PartnershipType Type,
    FuzzyDateDto? StartDate = null,
    FuzzyDateDto? EndDate = null,
    PartnershipEndReason? EndReason = null);

/// <summary>A problem with the seed file itself, reported with the entry that caused it.</summary>
public class SeedException(string message, Exception? inner = null) : Exception(message, inner);
