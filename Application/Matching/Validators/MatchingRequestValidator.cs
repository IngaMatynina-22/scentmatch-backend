using FluentValidation;
using ScentMatch.Application.Matching.DTOs;

namespace ScentMatch.Application.Matching.Validators;

public class MatchingRequestValidator : AbstractValidator<MatchingRequest>
{
    private const int MaxSelectedNotes = 50;

    public MatchingRequestValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneCriterion)
            .WithMessage("At least one matching criterion must be provided.")
            .WithName("criteria");

        RuleFor(x => x.SeasonId)
            .GreaterThan(0)
            .When(x => x.SeasonId.HasValue);

        RuleFor(x => x.FragranceTypeId)
            .GreaterThan(0)
            .When(x => x.FragranceTypeId.HasValue);

        RuleFor(x => x.Sweetness).InclusiveBetween(0, 100).When(x => x.Sweetness.HasValue);
        RuleFor(x => x.Freshness).InclusiveBetween(0, 100).When(x => x.Freshness.HasValue);
        RuleFor(x => x.Woody).InclusiveBetween(0, 100).When(x => x.Woody.HasValue);
        RuleFor(x => x.Spicy).InclusiveBetween(0, 100).When(x => x.Spicy.HasValue);
        RuleFor(x => x.Citrus).InclusiveBetween(0, 100).When(x => x.Citrus.HasValue);
        RuleFor(x => x.Floral).InclusiveBetween(0, 100).When(x => x.Floral.HasValue);
        RuleFor(x => x.Fruity).InclusiveBetween(0, 100).When(x => x.Fruity.HasValue);
        RuleFor(x => x.Powdery).InclusiveBetween(0, 100).When(x => x.Powdery.HasValue);
        RuleFor(x => x.Smoky).InclusiveBetween(0, 100).When(x => x.Smoky.HasValue);
        RuleFor(x => x.Oriental).InclusiveBetween(0, 100).When(x => x.Oriental.HasValue);

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

        RuleFor(x => x.SelectedNoteIds)
            .Must(ids => ids!.Count <= MaxSelectedNotes)
            .When(x => x.SelectedNoteIds is not null)
            .WithMessage($"selectedNoteIds must contain at most {MaxSelectedNotes} items.");

        RuleFor(x => x.SelectedNoteIds)
            .Must(ids => ids!.Distinct().Count() == ids.Count)
            .When(x => x.SelectedNoteIds is not null)
            .WithMessage("selectedNoteIds must not contain duplicates.");

        RuleForEach(x => x.SelectedNoteIds)
            .GreaterThan(0)
            .When(x => x.SelectedNoteIds is not null);
    }

    private static bool HasAtLeastOneCriterion(MatchingRequest request)
    {
        return request.SeasonId.HasValue
               || request.FragranceTypeId.HasValue
               || request.MinPrice.HasValue
               || request.MaxPrice.HasValue
               || (request.SelectedNoteIds is { Count: > 0 })
               || request.Sweetness.HasValue
               || request.Freshness.HasValue
               || request.Woody.HasValue
               || request.Spicy.HasValue
               || request.Citrus.HasValue
               || request.Floral.HasValue
               || request.Fruity.HasValue
               || request.Powdery.HasValue
               || request.Smoky.HasValue
               || request.Oriental.HasValue;
    }
}
