using ScentMatch.Application.Common.Scoring;
using ScentMatch.Infrastructure.Matching;

namespace ScentMatch.Tests.Matching;

public class PerfumeSimilarityCalculatorTests
{
    [Fact]
    public void Calculate_IdenticalProfiles_ScoresHigh()
    {
        var a = CreateProfile(
            id: 1,
            fragranceTypeId: 2,
            notes: [new ProfileNote(10, "Rose", "Top"), new ProfileNote(11, "Vanilla", "Base")],
            seasons: [new ProfileSeason(1, "Summer", 80)],
            sweetness: 70,
            freshness: 40,
            intensity: 60,
            price: 100m);

        var b = CreateProfile(
            id: 2,
            fragranceTypeId: 2,
            notes: [new ProfileNote(10, "Rose", "Top"), new ProfileNote(11, "Vanilla", "Base")],
            seasons: [new ProfileSeason(1, "Summer", 85)],
            sweetness: 72,
            freshness: 42,
            intensity: 62,
            price: 105m);

        var result = PerfumeSimilarityCalculator.Calculate(a, b);

        Assert.True(result.TotalScore >= 80m);
        Assert.Contains(result.PositiveReasons, r => r.Contains("fragrance type", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Components, c => c.Key == "Notes" && c.SkippedDueToMissingData);
    }

    [Fact]
    public void Calculate_MissingNotes_SkipsNotesComponent()
    {
        var a = CreateProfile(id: 1, fragranceTypeId: 1, notes: [], seasons: [], sweetness: 50);
        var b = CreateProfile(
            id: 2,
            fragranceTypeId: 1,
            notes: [new ProfileNote(1, "Bergamot", "Top")],
            seasons: [],
            sweetness: 50);

        var result = PerfumeSimilarityCalculator.Calculate(a, b);

        var notes = Assert.Single(result.Components, c => c.Key == "Notes");
        Assert.True(notes.SkippedDueToMissingData);
        Assert.Contains(result.DataGaps, g => g.Contains("notes", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Calculate_CharacteristicsOnlyWhereBothPresent()
    {
        var a = CreateProfile(id: 1, fragranceTypeId: 3, notes: [], seasons: [], sweetness: 90, freshness: null);
        var b = CreateProfile(id: 2, fragranceTypeId: 3, notes: [], seasons: [], sweetness: 10, freshness: 90);

        var result = PerfumeSimilarityCalculator.Calculate(a, b);

        Assert.Contains(result.Components, c => c.Key == "Characteristic:Sweetness" && !c.SkippedDueToMissingData);
        Assert.DoesNotContain(result.Components, c => c.Key == "Characteristic:Freshness");
        Assert.Contains(result.MismatchReasons, r => r.Contains("Sweetness", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Calculate_ExcludesSelfIsCallerResponsibility_DifferentIdsStillComparable()
    {
        var a = CreateProfile(id: 5, fragranceTypeId: 1, notes: [new ProfileNote(1, "Musk", "Base")], seasons: []);
        var b = CreateProfile(id: 5, fragranceTypeId: 1, notes: [new ProfileNote(1, "Musk", "Base")], seasons: []);

        var result = PerfumeSimilarityCalculator.Calculate(a, b);
        Assert.True(result.TotalScore > 0m);
    }

    [Fact]
    public void Calculate_DifferentFragranceType_AddsMismatch()
    {
        var a = CreateProfile(id: 1, fragranceTypeId: 1, fragranceTypeName: "Woody", notes: [], seasons: []);
        var b = CreateProfile(id: 2, fragranceTypeId: 2, fragranceTypeName: "Floral", notes: [], seasons: []);

        var result = PerfumeSimilarityCalculator.Calculate(a, b);

        Assert.Contains(result.MismatchReasons, r => r.Contains("fragrance type", StringComparison.OrdinalIgnoreCase));
        var type = Assert.Single(result.Components, c => c.Key == "FragranceType");
        Assert.Equal(0m, type.Similarity);
    }

    private static PerfumeProfile CreateProfile(
        int id,
        int fragranceTypeId,
        IReadOnlyList<ProfileNote> notes,
        IReadOnlyList<ProfileSeason> seasons,
        string fragranceTypeName = "Woody",
        int? sweetness = null,
        int? freshness = null,
        int? intensity = null,
        decimal? price = null) => new()
    {
        Id = id,
        Name = $"Perfume {id}",
        BrandName = "Brand",
        BrandId = 1,
        FragranceTypeId = fragranceTypeId,
        FragranceTypeName = fragranceTypeName,
        Sweetness = sweetness,
        Freshness = freshness,
        Intensity = intensity,
        Price = price,
        Notes = notes,
        Seasons = seasons
    };
}
