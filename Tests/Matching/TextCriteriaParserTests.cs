using ScentMatch.Infrastructure.Matching;

namespace ScentMatch.Tests.Matching;

public class TextCriteriaParserTests
{
    private static readonly TextCriteriaCatalog Catalog = new()
    {
        Notes =
        [
            new CatalogName(1, "Vanilla"),
            new CatalogName(2, "Rose"),
            new CatalogName(3, "Bergamot"),
            new CatalogName(4, "Orange Blossom")
        ],
        Seasons =
        [
            new CatalogName(10, "Summer"),
            new CatalogName(11, "Winter"),
            new CatalogName(12, "Spring"),
            new CatalogName(13, "Autumn")
        ],
        FragranceTypes =
        [
            new CatalogName(20, "Woody"),
            new CatalogName(21, "Floral"),
            new CatalogName(22, "Fresh")
        ]
    };

    [Fact]
    public void Parse_EnglishBudgetUnder_SetsMaxPriceAndCurrency()
    {
        var result = TextCriteriaParser.Parse("fresh scent under €80", Catalog);

        Assert.Equal(80m, result.Criteria.MaxPrice);
        Assert.Equal("EUR", result.Criteria.Currency);
        Assert.Equal(22, result.Criteria.FragranceTypeId);
    }

    [Fact]
    public void Parse_UkrainianBudgetDo_SetsMaxPrice()
    {
        var result = TextCriteriaParser.Parse("деревний аромат до 1500 грн", Catalog);

        Assert.Equal(1500m, result.Criteria.MaxPrice);
        Assert.Equal("UAH", result.Criteria.Currency);
        Assert.Equal(20, result.Criteria.FragranceTypeId);
    }

    [Fact]
    public void Parse_Seasons_UaAndEn()
    {
        var summer = TextCriteriaParser.Parse("для літа", Catalog);
        Assert.Equal(10, summer.Criteria.SeasonId);

        var winter = TextCriteriaParser.Parse("winter perfume", Catalog);
        Assert.Equal(11, winter.Criteria.SeasonId);
    }

    [Fact]
    public void Parse_WithoutNote_ExcludesNote()
    {
        var result = TextCriteriaParser.Parse("floral with rose without vanilla", Catalog);

        Assert.Contains(result.Criteria.PreferredNotes, n => n.Name == "Rose");
        Assert.Contains(result.Criteria.ExcludedNotes, n => n.Name == "Vanilla");
        Assert.DoesNotContain(result.Criteria.PreferredNotes, n => n.Name == "Vanilla");
    }

    [Fact]
    public void Parse_BezNote_UkrainianExclusion()
    {
        var result = TextCriteriaParser.Parse("аромат з bergamot без vanilla", Catalog);

        Assert.Contains(result.Criteria.PreferredNotes, n => n.Name == "Bergamot");
        Assert.Contains(result.Criteria.ExcludedNotes, n => n.Name == "Vanilla");
    }

    [Fact]
    public void Parse_IntensityAndConcentration()
    {
        var result = TextCriteriaParser.Parse("strong edp for women", Catalog);

        Assert.Equal(80, result.Criteria.Intensity);
        Assert.Equal("EDP", result.Criteria.Concentration);
        Assert.Equal("Female", result.Criteria.Gender);
    }

    [Fact]
    public void Parse_LongerNoteNamePreferred()
    {
        var result = TextCriteriaParser.Parse("orange blossom scent", Catalog);

        Assert.Contains(result.Criteria.PreferredNotes, n => n.Name == "Orange Blossom");
    }

    [Fact]
    public void ToMatchingRequest_MapsNotesAndBudget()
    {
        var parsed = TextCriteriaParser.Parse("woody under $100 with rose without vanilla", Catalog);
        var request = TextCriteriaParser.ToMatchingRequest(parsed.Criteria);

        Assert.Equal(100m, request.MaxPrice);
        Assert.Equal(20, request.FragranceTypeId);
        Assert.Contains(2, request.SelectedNoteIds!);
        Assert.Contains(1, request.ExcludedNoteIds!);
    }
}
