using FluentValidation;
using ScentMatch.Application.Notifications.DTOs;
using ScentMatch.Domain.Constants;

namespace ScentMatch.Application.Notifications.Validators;

public class CreateNotificationRequestValidator : AbstractValidator<CreateNotificationRequest>
{
    public CreateNotificationRequestValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(NotificationTypes.IsValid)
            .WithMessage($"Type must be one of: {NotificationTypes.AllowedList}.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Title must not be whitespace only.")
            .MaximumLength(200);

        RuleFor(x => x.Message)
            .NotEmpty()
            .Must(value => !string.IsNullOrWhiteSpace(value))
            .WithMessage("Message must not be whitespace only.")
            .MaximumLength(4000);

        RuleFor(x => x.PerfumeId)
            .GreaterThan(0)
            .When(x => x.PerfumeId.HasValue);
    }
}
