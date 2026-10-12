using FluentValidation;
using ScentMatch.Application.Analytics.DTOs;

namespace ScentMatch.Application.Analytics.Validators;

public class ResetPersonalizationRequestValidator : AbstractValidator<ResetPersonalizationRequest>
{
    public ResetPersonalizationRequestValidator()
    {
        // All fields optional; empty body is valid.
    }
}
