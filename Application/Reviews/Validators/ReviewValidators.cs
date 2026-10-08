using FluentValidation;
using ScentMatch.Application.Reviews.DTOs;
using ScentMatch.Domain.Constants;

namespace ScentMatch.Application.Reviews.Validators;

public class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Text)
            .MaximumLength(2000)
            .When(x => x.Text is not null);
    }
}

public class UpdateReviewRequestValidator : AbstractValidator<UpdateReviewRequest>
{
    public UpdateReviewRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Text)
            .MaximumLength(2000)
            .When(x => x.Text is not null);
    }
}

public class AdminReviewQueryParametersValidator : AbstractValidator<AdminReviewQueryParameters>
{
    public AdminReviewQueryParametersValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("page must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("pageSize must be between 1 and 100.");

        RuleFor(x => x.Status)
            .Must(status => status is null || ReviewStatuses.IsValid(status))
            .WithMessage("status must be Pending, Approved, or Rejected.");
    }
}

public class ReviewQueryParametersValidator : AbstractValidator<ReviewQueryParameters>
{
    private static readonly HashSet<string> AllowedSortBy = new(StringComparer.OrdinalIgnoreCase)
    {
        "date",
        "rating"
    };

    private static readonly HashSet<string> AllowedSortOrder = new(StringComparer.OrdinalIgnoreCase)
    {
        "asc",
        "desc"
    };

    public ReviewQueryParametersValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("page must be greater than or equal to 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("pageSize must be between 1 and 100.");

        RuleFor(x => x.SortBy)
            .Must(value => string.IsNullOrWhiteSpace(value) || AllowedSortBy.Contains(value.Trim()))
            .WithMessage("sortBy must be one of: date, rating.");

        RuleFor(x => x.SortOrder)
            .Must(value => string.IsNullOrWhiteSpace(value) || AllowedSortOrder.Contains(value.Trim()))
            .WithMessage("sortOrder must be one of: asc, desc.");
    }
}
