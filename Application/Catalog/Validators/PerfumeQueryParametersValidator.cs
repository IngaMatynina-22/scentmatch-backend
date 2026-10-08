using FluentValidation;
using ScentMatch.Application.Catalog.DTOs;

namespace ScentMatch.Application.Catalog.Validators;

public class PerfumeQueryParametersValidator : AbstractValidator<PerfumeQueryParameters>
{
    private static readonly HashSet<string> AllowedSortBy = new(StringComparer.OrdinalIgnoreCase)
    {
        "name",
        "price",
        "rating",
        "releaseYear"
    };

    private static readonly HashSet<string> AllowedSortOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        "asc",
        "desc"
    };

    public PerfumeQueryParametersValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("page must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("pageSize must be between 1 and 100.");

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinPrice.HasValue)
            .WithMessage("minPrice must be greater than or equal to 0.");

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxPrice.HasValue)
            .WithMessage("maxPrice must be greater than or equal to 0.");

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage("minPrice must be less than or equal to maxPrice.")
            .WithName("minPrice");

        RuleFor(x => x.MinRating)
            .InclusiveBetween(0, 5)
            .When(x => x.MinRating.HasValue)
            .WithMessage("minRating must be between 0 and 5.");

        RuleFor(x => x.SortBy)
            .Must(value => string.IsNullOrWhiteSpace(value) || AllowedSortBy.Contains(value.Trim()))
            .WithMessage("sortBy must be one of: name, price, rating, releaseYear.");

        RuleFor(x => x.SortOrder)
            .Must(value => string.IsNullOrWhiteSpace(value) || AllowedSortOrder.Contains(value.Trim()))
            .WithMessage("sortOrder must be one of: asc, desc.");

        RuleFor(x => x.NoteIds)
            .Must(BeValidNoteIdsFormat)
            .When(x => !string.IsNullOrWhiteSpace(x.NoteIds))
            .WithMessage("noteIds must be a comma-separated list of positive integers.");

        RuleFor(x => x.BrandId)
            .GreaterThan(0)
            .When(x => x.BrandId.HasValue);

        RuleFor(x => x.FragranceTypeId)
            .GreaterThan(0)
            .When(x => x.FragranceTypeId.HasValue);

        RuleFor(x => x.SeasonId)
            .GreaterThan(0)
            .When(x => x.SeasonId.HasValue);
    }

    private static bool BeValidNoteIdsFormat(string? noteIds)
    {
        if (string.IsNullOrWhiteSpace(noteIds))
        {
            return true;
        }

        var parts = noteIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        return parts.All(part => int.TryParse(part, out var id) && id > 0);
    }
}
