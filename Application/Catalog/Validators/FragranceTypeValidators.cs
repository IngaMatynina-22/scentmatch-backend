using FluentValidation;
using ScentMatch.Application.Catalog.DTOs;

namespace ScentMatch.Application.Catalog.Validators;

public class CreateFragranceTypeRequestValidator : AbstractValidator<CreateFragranceTypeRequest>
{
    public CreateFragranceTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);
    }
}

public class UpdateFragranceTypeRequestValidator : AbstractValidator<UpdateFragranceTypeRequest>
{
    public UpdateFragranceTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100);
    }
}
