using FluentValidation;
using ScentMatch.Application.Library.DTOs;
using ScentMatch.Domain.Constants;

namespace ScentMatch.Application.Library.Validators;

public class CreateWishlistItemRequestValidator : AbstractValidator<CreateWishlistItemRequest>
{
    public CreateWishlistItemRequestValidator()
    {
        RuleFor(x => x.PerfumeId).GreaterThan(0);
        RuleFor(x => x.Status)
            .Must(status => status is null || WishlistStatuses.IsValid(status))
            .WithMessage($"Status must be one of: {WishlistStatuses.AllowedList}.");
        RuleFor(x => x.TargetPrice).GreaterThan(0).When(x => x.TargetPrice.HasValue);
        RuleFor(x => x.Currency)
            .Length(3)
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.Note).MaximumLength(1000).When(x => x.Note is not null);
    }
}

public class UpdateWishlistItemRequestValidator : AbstractValidator<UpdateWishlistItemRequest>
{
    public UpdateWishlistItemRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(WishlistStatuses.IsValid!)
            .WithMessage($"Status must be one of: {WishlistStatuses.AllowedList}.");
        RuleFor(x => x.TargetPrice).GreaterThan(0).When(x => x.TargetPrice.HasValue);
        RuleFor(x => x.Currency)
            .Length(3)
            .When(x => !string.IsNullOrWhiteSpace(x.Currency));
        RuleFor(x => x.Note).MaximumLength(1000).When(x => x.Note is not null);
    }
}

public class WishlistQueryParametersValidator : AbstractValidator<WishlistQueryParameters>
{
    public WishlistQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status)
            .Must(status => status is null || WishlistStatuses.IsValid(status))
            .WithMessage($"Status must be one of: {WishlistStatuses.AllowedList}.");
    }
}
