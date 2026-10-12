using FluentValidation;
using ScentMatch.Application.Library.DTOs;

namespace ScentMatch.Application.Library.Validators;

public class CreateBlindComparisonRequestValidator : AbstractValidator<CreateBlindComparisonRequest>
{
    public CreateBlindComparisonRequestValidator()
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

public class BlindComparisonRatingsRequestValidator : AbstractValidator<BlindComparisonRatingsRequest>
{
    public BlindComparisonRatingsRequestValidator()
    {
        RuleFor(x => x.Ratings)
            .NotNull()
            .NotEmpty()
            .WithMessage("ratings is required.");

        RuleForEach(x => x.Ratings).ChildRules(item =>
        {
            item.RuleFor(x => x.Label)
                .NotEmpty()
                .MaximumLength(10);
            item.RuleFor(x => x.Rating)
                .InclusiveBetween(1, 10)
                .When(x => x.Rating.HasValue);
            item.RuleFor(x => x.Comment)
                .MaximumLength(1000)
                .When(x => x.Comment is not null);
        });
    }
}

public class CompleteBlindComparisonRequestValidator : AbstractValidator<CompleteBlindComparisonRequest>
{
    public CompleteBlindComparisonRequestValidator()
    {
        RuleFor(x => x.WinnerLabel)
            .MaximumLength(10)
            .When(x => x.WinnerLabel is not null);
    }
}
