using FluentValidation;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Domain.Constants;

namespace ScentMatch.Application.Library.Validators;

public class AssignWardrobeRequestValidator : AbstractValidator<AssignWardrobeRequest>
{
    public AssignWardrobeRequestValidator()
    {
        RuleFor(x => x.PerfumeId).GreaterThan(0);
        RuleFor(x => x.ScenarioKey)
            .NotEmpty()
            .Must(WardrobeScenarios.IsValid!)
            .WithMessage($"ScenarioKey must be one of: {WardrobeScenarios.AllowedList}.");
        RuleFor(x => x.CollectionName)
            .MaximumLength(200)
            .When(x => x.CollectionName is not null);
    }
}
