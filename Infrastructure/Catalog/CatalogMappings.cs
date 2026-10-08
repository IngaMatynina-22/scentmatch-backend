using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Domain.Entities;

namespace ScentMatch.Infrastructure.Catalog;

internal static class CatalogMappings
{
    public static BrandDto ToDto(Brand brand) => new()
    {
        Id = brand.Id,
        Name = brand.Name,
        Description = brand.Description,
        Country = brand.Country,
        Website = brand.Website,
        LogoUrl = brand.LogoUrl
    };

    public static FragranceTypeDto ToDto(FragranceType entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description
    };

    public static SeasonDto ToDto(Season entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description
    };

    public static NoteDto ToDto(Note entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        Category = entity.Category
    };

    public static PerfumeCharacteristicsDto ToCharacteristics(Perfume perfume) => new()
    {
        Sweetness = perfume.Sweetness,
        Freshness = perfume.Freshness,
        Woody = perfume.Woody,
        Spicy = perfume.Spicy,
        Citrus = perfume.Citrus,
        Floral = perfume.Floral,
        Fruity = perfume.Fruity,
        Powdery = perfume.Powdery,
        Smoky = perfume.Smoky,
        Oriental = perfume.Oriental
    };

    public static PerfumeListItemDto ToListItemDto(Perfume perfume) => new()
    {
        Id = perfume.Id,
        Name = perfume.Name,
        Brand = new NamedEntityDto { Id = perfume.Brand.Id, Name = perfume.Brand.Name },
        FragranceType = new NamedEntityDto { Id = perfume.FragranceType.Id, Name = perfume.FragranceType.Name },
        Description = perfume.Description,
        ReleaseYear = perfume.ReleaseYear,
        Concentration = perfume.Concentration,
        Gender = perfume.Gender,
        Price = perfume.Price,
        ImageUrl = perfume.ImageUrl,
        Rating = perfume.Rating,
        ReviewsCount = perfume.ReviewsCount,
        Characteristics = ToCharacteristics(perfume),
        Notes = perfume.PerfumeNotes
            .OrderBy(x => x.NoteType)
            .ThenBy(x => x.Note.Name)
            .Select(x => new PerfumeNoteDto
            {
                NoteId = x.NoteId,
                Name = x.Note.Name,
                NoteType = x.NoteType
            })
            .ToList(),
        Seasons = perfume.PerfumeSeasons
            .OrderBy(x => x.Season.Name)
            .Select(x => new PerfumeSeasonDto
            {
                SeasonId = x.SeasonId,
                Name = x.Season.Name,
                Suitability = x.Suitability
            })
            .ToList()
    };

    public static PerfumeDetailsDto ToDetailsDto(Perfume perfume)
    {
        var listItem = ToListItemDto(perfume);
        return new PerfumeDetailsDto
        {
            Id = listItem.Id,
            Name = listItem.Name,
            Brand = listItem.Brand,
            FragranceType = listItem.FragranceType,
            Description = listItem.Description,
            ReleaseYear = listItem.ReleaseYear,
            Concentration = listItem.Concentration,
            Gender = listItem.Gender,
            Price = listItem.Price,
            ImageUrl = listItem.ImageUrl,
            Rating = listItem.Rating,
            ReviewsCount = listItem.ReviewsCount,
            Characteristics = listItem.Characteristics,
            Notes = listItem.Notes,
            Seasons = listItem.Seasons
        };
    }

    public static void ApplyCharacteristics(Perfume perfume, PerfumeCharacteristicsDto? characteristics)
    {
        if (characteristics is null)
        {
            return;
        }

        perfume.Sweetness = characteristics.Sweetness;
        perfume.Freshness = characteristics.Freshness;
        perfume.Woody = characteristics.Woody;
        perfume.Spicy = characteristics.Spicy;
        perfume.Citrus = characteristics.Citrus;
        perfume.Floral = characteristics.Floral;
        perfume.Fruity = characteristics.Fruity;
        perfume.Powdery = characteristics.Powdery;
        perfume.Smoky = characteristics.Smoky;
        perfume.Oriental = characteristics.Oriental;
    }
}
