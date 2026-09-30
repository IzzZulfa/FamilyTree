using family_tree.Data;
using family_tree.Dtos;
using family_tree.Entities;
using Microsoft.EntityFrameworkCore;

namespace family_tree.Services;

public class PartnershipService(FamilyTreeDbContext db)
{
    public async Task<SaveResponse<PartnershipResponse>> AddAsync(
        int personId, AddPartnershipRequest request, CancellationToken ct)
    {
        var person = await db.People.FindAsync([personId], ct)
            ?? throw new NotFoundException($"Person {personId} does not exist.");

        // Rule 3: no self-relationships.
        if (request.PartnerId == personId)
        {
            throw new ValidationException(ValidationCodes.SelfRelationship, "A person cannot be their own partner.");
        }

        var partner = await db.People.FindAsync([request.PartnerId], ct)
            ?? throw new ValidationException(ValidationCodes.PersonNotFound,
                $"Partner {request.PartnerId} does not exist.");

        // Rule 6: both people must belong to the same tree.
        if (partner.TreeId != person.TreeId)
        {
            throw new ValidationException(ValidationCodes.DifferentTrees,
                "The two partners belong to different trees.");
        }

        if (request.StartDate is not null && request.EndDate is not null
            && request.EndDate.Value < request.StartDate.Value)
        {
            throw new ValidationException(ValidationCodes.InvalidDateRange,
                "The partnership's end date is before its start date.");
        }

        // Rule 4: no overlapping partnerships between the same pair (stored in either order).
        var existing = await db.Partnerships
            .Where(p => (p.Person1Id == personId && p.Person2Id == partner.Id)
                     || (p.Person1Id == partner.Id && p.Person2Id == personId))
            .ToListAsync(ct);
        if (existing.Any(p => Overlaps(p.StartDate, p.EndDate, request.StartDate?.Value, request.EndDate?.Value)))
        {
            throw new ValidationException(ValidationCodes.OverlappingPartnership,
                "These two people already have a partnership whose dates overlap this one.");
        }

        var partnership = new Partnership
        {
            Person1Id = personId,
            Person2Id = partner.Id,
            Type = request.Type,
            StartDate = request.StartDate?.ToEntity(),
            EndDate = request.EndDate?.ToEntity(),
            EndReason = request.EndReason,
        };
        db.Partnerships.Add(partnership);
        await db.SaveChangesAsync(ct);

        // Rule 5's date checks don't cover partnerships, but the response keeps the same
        // shape as other relationship endpoints so clients can handle warnings uniformly.
        return new SaveResponse<PartnershipResponse>(PartnershipResponse.From(partnership), []);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var deleted = await db.Partnerships.Where(p => p.Id == id).ExecuteDeleteAsync(ct);
        if (deleted == 0)
        {
            throw new NotFoundException($"Partnership {id} does not exist.");
        }
    }

    /// <summary>
    /// Do two date ranges overlap? A missing start means "since forever" and a missing end means
    /// "still ongoing", so two undated partnerships always overlap. Ranges that merely touch
    /// (one ends the day the next starts) don't count as overlapping.
    /// </summary>
    private static bool Overlaps(FuzzyDate? existingStart, FuzzyDate? existingEnd, DateOnly? newStart, DateOnly? newEnd)
    {
        var aStart = existingStart?.Value ?? DateOnly.MinValue;
        var aEnd = existingEnd?.Value ?? DateOnly.MaxValue;
        var bStart = newStart ?? DateOnly.MinValue;
        var bEnd = newEnd ?? DateOnly.MaxValue;
        return aStart < bEnd && bStart < aEnd;
    }
}
