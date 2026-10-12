using FluentValidation;
using ScentMatch.Application.Matching.DTOs;

namespace ScentMatch.Application.Matching.Validators;

public class SimilarPerfumesQueryValidator : AbstractValidator<SimilarPerfumesQuery>
{
    public SimilarPerfumesQueryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .When(x => x.Limit.HasValue);

        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
