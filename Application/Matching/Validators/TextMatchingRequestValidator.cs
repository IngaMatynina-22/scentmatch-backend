using FluentValidation;
using ScentMatch.Application.Matching.DTOs;

namespace ScentMatch.Application.Matching.Validators;

public class TextMatchingRequestValidator : AbstractValidator<TextMatchingRequest>
{
    public TextMatchingRequestValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.MaxResults)
            .InclusiveBetween(1, 50);
    }
}
