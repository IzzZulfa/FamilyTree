using System.ComponentModel.DataAnnotations;
using family_tree.Entities;

namespace family_tree.Dtos;

/// <summary>Body for both creating (POST) and replacing (PUT) a person.</summary>
public record SavePersonRequest(
    [Required, StringLength(200)] string GivenName,
    [StringLength(200)] string? Surname = null,
    [StringLength(200)] string? BirthSurname = null,
    Sex Sex = Sex.Unknown,
    FuzzyDateDto? BirthDate = null,
    [StringLength(200)] string? BirthPlace = null,
    FuzzyDateDto? DeathDate = null,
    string? Notes = null);

public record PersonResponse(
    int Id,
    int TreeId,
    string GivenName,
    string Surname,
    string? BirthSurname,
    Sex Sex,
    FuzzyDateDto? BirthDate,
    string? BirthPlace,
    FuzzyDateDto? DeathDate,
    string? Notes)
{
    public static PersonResponse From(Person person) => new(
        person.Id,
        person.TreeId,
        person.GivenName,
        person.Surname,
        person.BirthSurname,
        person.Sex,
        FuzzyDateDto.From(person.BirthDate),
        person.BirthPlace,
        FuzzyDateDto.From(person.DeathDate),
        person.Notes);
}
