using FluentValidation;
using ScentMatch.Application.Library.DTOs;

namespace ScentMatch.Application.Library.Validators;

public class CreateJournalEntryRequestValidator : AbstractValidator<CreateJournalEntryRequest>
{
    public CreateJournalEntryRequestValidator()
    {
        RuleFor(x => x.PerfumeId).GreaterThan(0);
        RuleFor(x => x.Rating).InclusiveBetween(1, 10);
        RuleFor(x => x.Notes).MaximumLength(4000).When(x => x.Notes is not null);
        RuleFor(x => x.WearMinutes).GreaterThan(0).When(x => x.WearMinutes.HasValue);
        RuleFor(x => x.OpeningImpression).MaximumLength(1000).When(x => x.OpeningImpression is not null);
        RuleFor(x => x.HeartImpression).MaximumLength(1000).When(x => x.HeartImpression is not null);
        RuleFor(x => x.BaseImpression).MaximumLength(1000).When(x => x.BaseImpression is not null);
        RuleForEach(x => x.Tags)
            .MaximumLength(50)
            .When(x => x.Tags is not null);
        RuleFor(x => x.Tags)
            .Must(tags => tags is null || tags.Count <= 20)
            .WithMessage("At most 20 tags are allowed.");
    }
}

public class UpdateJournalEntryRequestValidator : AbstractValidator<UpdateJournalEntryRequest>
{
    public UpdateJournalEntryRequestValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 10);
        RuleFor(x => x.Notes).MaximumLength(4000).When(x => x.Notes is not null);
        RuleFor(x => x.WearMinutes).GreaterThan(0).When(x => x.WearMinutes.HasValue);
        RuleFor(x => x.OpeningImpression).MaximumLength(1000).When(x => x.OpeningImpression is not null);
        RuleFor(x => x.HeartImpression).MaximumLength(1000).When(x => x.HeartImpression is not null);
        RuleFor(x => x.BaseImpression).MaximumLength(1000).When(x => x.BaseImpression is not null);
        RuleForEach(x => x.Tags)
            .MaximumLength(50)
            .When(x => x.Tags is not null);
        RuleFor(x => x.Tags)
            .Must(tags => tags is null || tags.Count <= 20)
            .WithMessage("At most 20 tags are allowed.");
    }
}

public class JournalQueryParametersValidator : AbstractValidator<JournalQueryParameters>
{
    public JournalQueryParametersValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.PerfumeId).GreaterThan(0).When(x => x.PerfumeId.HasValue);
        RuleFor(x => x.Rating).InclusiveBetween(1, 10).When(x => x.Rating.HasValue);
        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithMessage("from must be less than or equal to to.");
    }
}
