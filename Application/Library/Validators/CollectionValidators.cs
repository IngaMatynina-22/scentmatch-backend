using FluentValidation;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Domain.Constants;

namespace ScentMatch.Application.Library.Validators;

public class CreateCollectionRequestValidator : AbstractValidator<CreateCollectionRequest>
{
    public CreateCollectionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Name must not be whitespace only.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);

        RuleFor(x => x.ScenarioKey)
            .Must(key => key is null || WardrobeScenarios.IsValid(key))
            .WithMessage($"ScenarioKey must be null or one of: {WardrobeScenarios.AllowedList}.");
    }
}

public class UpdateCollectionRequestValidator : AbstractValidator<UpdateCollectionRequest>
{
    public UpdateCollectionRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Name must not be whitespace only.")
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);

        RuleFor(x => x.ScenarioKey)
            .Must(key => key is null || WardrobeScenarios.IsValid(key))
            .WithMessage($"ScenarioKey must be null or one of: {WardrobeScenarios.AllowedList}.");
    }
}
