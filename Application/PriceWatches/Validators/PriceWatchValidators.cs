using FluentValidation;
using ScentMatch.Application.PriceWatches.DTOs;

namespace ScentMatch.Application.PriceWatches.Validators;

public class CreatePriceWatchRequestValidator : AbstractValidator<CreatePriceWatchRequest>
{
    public CreatePriceWatchRequestValidator()
    {
        RuleFor(x => x.PerfumeId).GreaterThan(0);
        RuleFor(x => x.TargetPrice).GreaterThan(0);
        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Currency must be a 3-letter ISO code.");
        RuleFor(x => x.ShopLinkId).GreaterThan(0).When(x => x.ShopLinkId.HasValue);
        RuleFor(x => x.SourceUrl)
            .MaximumLength(1000)
            .Must(BeValidHttpUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.SourceUrl))
            .WithMessage("SourceUrl must be a valid http or https address.");
        RuleFor(x => x.SourceName).MaximumLength(100).When(x => x.SourceName is not null);
    }

    private static bool BeValidHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public class UpdatePriceWatchRequestValidator : AbstractValidator<UpdatePriceWatchRequest>
{
    public UpdatePriceWatchRequestValidator()
    {
        RuleFor(x => x.TargetPrice).GreaterThan(0).When(x => x.TargetPrice.HasValue);
        RuleFor(x => x.Currency)
            .Length(3)
            .Matches("^[A-Za-z]{3}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Currency))
            .WithMessage("Currency must be a 3-letter ISO code.");
        RuleFor(x => x.ShopLinkId).GreaterThan(0).When(x => x.ShopLinkId.HasValue);
        RuleFor(x => x.SourceUrl)
            .MaximumLength(1000)
            .Must(BeValidHttpUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.SourceUrl))
            .WithMessage("SourceUrl must be a valid http or https address.");
        RuleFor(x => x.SourceName).MaximumLength(100).When(x => x.SourceName is not null);

        RuleFor(x => x)
            .Must(x => x.TargetPrice.HasValue
                       || x.IsActive.HasValue
                       || !string.IsNullOrWhiteSpace(x.Currency)
                       || x.ShopLinkId.HasValue
                       || x.SourceUrl is not null
                       || x.SourceName is not null)
            .WithMessage("At least one field must be provided to update.");
    }

    private static bool BeValidHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
