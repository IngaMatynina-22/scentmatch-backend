using FluentValidation;
using ScentMatch.Application.Profile.DTOs;

namespace ScentMatch.Application.Profile.Validators;

public class UpdateUserPreferencesRequestValidator : AbstractValidator<UpdateUserPreferencesRequest>
{
    public UpdateUserPreferencesRequestValidator()
    {
        RuleFor(x => x.PreferredSeasonId)
            .GreaterThan(0)
            .When(x => x.PreferredSeasonId.HasValue);

        RuleFor(x => x.PreferredFragranceTypeId)
            .GreaterThan(0)
            .When(x => x.PreferredFragranceTypeId.HasValue);

        RuleFor(x => x.PreferredNoteIds)
            .Must(ids => ids is null || (ids.Count <= 20 && ids.All(id => id > 0)))
            .WithMessage("preferredNoteIds must contain at most 20 positive integers.")
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Count)
            .WithMessage("preferredNoteIds must not contain duplicates.");

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
    }
}
