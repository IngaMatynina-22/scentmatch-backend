using FluentValidation;
using ScentMatch.Application.Profile.DTOs;

namespace ScentMatch.Application.Profile.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Username must not be whitespace only.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
            .MaximumLength(100).WithMessage("Username must not exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9._-]+$").WithMessage("Username contains invalid characters.");

        RuleFor(x => x.FirstName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.FirstName));

        RuleFor(x => x.LastName)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.LastName));

        RuleFor(x => x.AvatarUrl)
            .MaximumLength(500)
            .Must(BeValidUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.AvatarUrl))
            .WithMessage("AvatarUrl must be a valid absolute URL.");
    }

    private static bool BeValidUrl(string? url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
