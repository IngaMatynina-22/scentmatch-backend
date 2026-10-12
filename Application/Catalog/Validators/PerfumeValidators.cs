using FluentValidation;
using ScentMatch.Application.Catalog.DTOs;
using ScentMatch.Domain.Constants;

namespace ScentMatch.Application.Catalog.Validators;

public class CreatePerfumeRequestValidator : AbstractValidator<CreatePerfumeRequest>
{
    public CreatePerfumeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(250);

        RuleFor(x => x.BrandId).GreaterThan(0);
        RuleFor(x => x.FragranceTypeId).GreaterThan(0);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Price.HasValue);

        RuleFor(x => x.ReleaseYear)
            .InclusiveBetween(1800, DateTime.UtcNow.Year + 1)
            .When(x => x.ReleaseYear.HasValue);

        RuleFor(x => x.Concentration).MaximumLength(50).When(x => x.Concentration is not null);
        RuleFor(x => x.Gender).MaximumLength(50).When(x => x.Gender is not null);
        RuleFor(x => x.ImageUrl).MaximumLength(1000).When(x => x.ImageUrl is not null);

        RuleFor(x => x.Characteristics!).SetValidator(new PerfumeCharacteristicsDtoValidator())
            .When(x => x.Characteristics is not null);

        RuleForEach(x => x.Notes).SetValidator(new PerfumeNoteInputDtoValidator());
        RuleForEach(x => x.Seasons).SetValidator(new PerfumeSeasonInputDtoValidator());
        RuleForEach(x => x.ShopLinks).SetValidator(new PerfumeShopLinkInputDtoValidator());

        RuleFor(x => x.Notes)
            .Must(notes => notes.Select(n => n.NoteId).Distinct().Count() == notes.Count)
            .WithMessage("Duplicate note IDs are not allowed.");

        RuleFor(x => x.Seasons)
            .Must(seasons => seasons.Select(s => s.SeasonId).Distinct().Count() == seasons.Count)
            .WithMessage("Duplicate season IDs are not allowed.");
    }
}

public class UpdatePerfumeRequestValidator : AbstractValidator<UpdatePerfumeRequest>
{
    public UpdatePerfumeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(250);

        RuleFor(x => x.BrandId).GreaterThan(0);
        RuleFor(x => x.FragranceTypeId).GreaterThan(0);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Price.HasValue);

        RuleFor(x => x.ReleaseYear)
            .InclusiveBetween(1800, DateTime.UtcNow.Year + 1)
            .When(x => x.ReleaseYear.HasValue);

        RuleFor(x => x.Concentration).MaximumLength(50).When(x => x.Concentration is not null);
        RuleFor(x => x.Gender).MaximumLength(50).When(x => x.Gender is not null);
        RuleFor(x => x.ImageUrl).MaximumLength(1000).When(x => x.ImageUrl is not null);

        RuleFor(x => x.Characteristics!).SetValidator(new PerfumeCharacteristicsDtoValidator())
            .When(x => x.Characteristics is not null);

        RuleForEach(x => x.Notes).SetValidator(new PerfumeNoteInputDtoValidator());
        RuleForEach(x => x.Seasons).SetValidator(new PerfumeSeasonInputDtoValidator());
        RuleForEach(x => x.ShopLinks).SetValidator(new PerfumeShopLinkInputDtoValidator());

        RuleFor(x => x.Notes)
            .Must(notes => notes.Select(n => n.NoteId).Distinct().Count() == notes.Count)
            .WithMessage("Duplicate note IDs are not allowed.");

        RuleFor(x => x.Seasons)
            .Must(seasons => seasons.Select(s => s.SeasonId).Distinct().Count() == seasons.Count)
            .WithMessage("Duplicate season IDs are not allowed.");
    }
}

public class PerfumeCharacteristicsDtoValidator : AbstractValidator<PerfumeCharacteristicsDto>
{
    public PerfumeCharacteristicsDtoValidator()
    {
        RuleFor(x => x.Sweetness).InclusiveBetween(0, 100).When(x => x.Sweetness.HasValue);
        RuleFor(x => x.Freshness).InclusiveBetween(0, 100).When(x => x.Freshness.HasValue);
        RuleFor(x => x.Woody).InclusiveBetween(0, 100).When(x => x.Woody.HasValue);
        RuleFor(x => x.Spicy).InclusiveBetween(0, 100).When(x => x.Spicy.HasValue);
        RuleFor(x => x.Citrus).InclusiveBetween(0, 100).When(x => x.Citrus.HasValue);
        RuleFor(x => x.Floral).InclusiveBetween(0, 100).When(x => x.Floral.HasValue);
        RuleFor(x => x.Fruity).InclusiveBetween(0, 100).When(x => x.Fruity.HasValue);
        RuleFor(x => x.Powdery).InclusiveBetween(0, 100).When(x => x.Powdery.HasValue);
        RuleFor(x => x.Smoky).InclusiveBetween(0, 100).When(x => x.Smoky.HasValue);
        RuleFor(x => x.Oriental).InclusiveBetween(0, 100).When(x => x.Oriental.HasValue);
    }
}

public class PerfumeNoteInputDtoValidator : AbstractValidator<PerfumeNoteInputDto>
{
    public PerfumeNoteInputDtoValidator()
    {
        RuleFor(x => x.NoteId).GreaterThan(0);
        RuleFor(x => x.NoteType)
            .NotEmpty()
            .Must(NoteTypes.IsValid)
            .WithMessage("NoteType must be Top, Middle, or Base.");
    }
}

public class PerfumeSeasonInputDtoValidator : AbstractValidator<PerfumeSeasonInputDto>
{
    public PerfumeSeasonInputDtoValidator()
    {
        RuleFor(x => x.SeasonId).GreaterThan(0);
        RuleFor(x => x.Suitability).InclusiveBetween(0, 100);
    }
}

public class PerfumeShopLinkInputDtoValidator : AbstractValidator<PerfumeShopLinkInputDto>
{
    public PerfumeShopLinkInputDtoValidator()
    {
        RuleFor(x => x.StoreName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(1000)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri)
                         && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Url must be a valid http or https address.");
    }
}
