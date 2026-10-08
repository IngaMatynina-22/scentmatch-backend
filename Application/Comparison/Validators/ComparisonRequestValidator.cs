using FluentValidation;
using ScentMatch.Application.Comparison.DTOs;

namespace ScentMatch.Application.Comparison.Validators;

public class ComparisonRequestValidator : AbstractValidator<ComparisonRequest>
{
    public ComparisonRequestValidator()
    {
        RuleFor(x => x.PerfumeIds)
            .NotNull()
            .WithMessage("perfumeIds is required.");

        RuleFor(x => x.PerfumeIds)
            .Must(ids => ids.Count is >= 2 and <= 5)
            .When(x => x.PerfumeIds is not null)
            .WithMessage("perfumeIds must contain between 2 and 5 items.");

        RuleFor(x => x.PerfumeIds)
            .Must(ids => ids.All(id => id > 0))
            .When(x => x.PerfumeIds is not null)
            .WithMessage("perfumeIds must contain only positive values.");

        RuleFor(x => x.PerfumeIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .When(x => x.PerfumeIds is not null)
            .WithMessage("perfumeIds must not contain duplicates.");
    }
}
