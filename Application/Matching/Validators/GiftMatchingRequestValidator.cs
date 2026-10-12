using FluentValidation;
using ScentMatch.Application.Matching.DTOs;

namespace ScentMatch.Application.Matching.Validators;

public class GiftMatchingRequestValidator : AbstractValidator<GiftMatchingRequest>
{
    private const int MaxNotes = 50;

    public GiftMatchingRequestValidator()
    {
        RuleFor(x => x)
            .Must(HasAtLeastOneCriterion)
            .WithMessage("At least one gift matching criterion must be provided.")
            .WithName("criteria");

        RuleFor(x => x.SeasonId).GreaterThan(0).When(x => x.SeasonId.HasValue);
        RuleFor(x => x.FragranceTypeId).GreaterThan(0).When(x => x.FragranceTypeId.HasValue);
        RuleFor(x => x.Intensity).InclusiveBetween(0, 100).When(x => x.Intensity.HasValue);
        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);
        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage("minPrice must be less than or equal to maxPrice.")
            .WithName("minPrice");

        RuleFor(x => x.Currency).MaximumLength(8).When(x => x.Currency is not null);
        RuleFor(x => x.Occasion).MaximumLength(100).When(x => x.Occasion is not null);
        RuleFor(x => x.Gender).MaximumLength(30).When(x => x.Gender is not null);
        RuleFor(x => x.MaxResults).InclusiveBetween(1, 50);

        RuleFor(x => x.PreferredNoteIds)
            .Must(ids => ids!.Count <= MaxNotes)
            .When(x => x.PreferredNoteIds is not null)
            .WithMessage($"preferredNoteIds must contain at most {MaxNotes} items.");

        RuleFor(x => x.ExcludedNoteIds)
            .Must(ids => ids!.Count <= MaxNotes)
            .When(x => x.ExcludedNoteIds is not null)
            .WithMessage($"excludedNoteIds must contain at most {MaxNotes} items.");

        RuleForEach(x => x.PreferredNoteIds).GreaterThan(0).When(x => x.PreferredNoteIds is not null);
        RuleForEach(x => x.ExcludedNoteIds).GreaterThan(0).When(x => x.ExcludedNoteIds is not null);
    }

    private static bool HasAtLeastOneCriterion(GiftMatchingRequest request)
    {
        return request.MinPrice.HasValue
               || request.MaxPrice.HasValue
               || request.SeasonId.HasValue
               || request.FragranceTypeId.HasValue
               || request.Intensity.HasValue
               || !string.IsNullOrWhiteSpace(request.Gender)
               || !string.IsNullOrWhiteSpace(request.Occasion)
               || (request.PreferredNoteIds is { Count: > 0 })
               || (request.ExcludedNoteIds is { Count: > 0 });
    }
}
