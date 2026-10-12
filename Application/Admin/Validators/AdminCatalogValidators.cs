using FluentValidation;
using ScentMatch.Application.Admin.DTOs;

namespace ScentMatch.Application.Admin.Validators;

public class CatalogImportRequestValidator : AbstractValidator<CatalogImportRequest>
{
    public CatalogImportRequestValidator()
    {
        RuleFor(x => x.Rows)
            .NotNull()
            .Must(x => x.Count > 0)
            .WithMessage("At least one import row is required.")
            .Must(x => x.Count <= 500)
            .WithMessage("A maximum of 500 rows can be imported at once.");

        RuleForEach(x => x.Rows).SetValidator(new CatalogImportRowDtoValidator());
    }
}

public class CatalogImportRowDtoValidator : AbstractValidator<CatalogImportRowDto>
{
    public CatalogImportRowDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(250);

        RuleFor(x => x.BrandName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.FragranceTypeName).MaximumLength(100).When(x => x.FragranceTypeName is not null);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price.HasValue);
        RuleFor(x => x.Currency).MaximumLength(3).When(x => x.Currency is not null);
        RuleFor(x => x.Barcode)
            .Matches(@"^[0-9]{8,14}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Barcode));
        RuleFor(x => x.ImageUrl).MaximumLength(1000).When(x => x.ImageUrl is not null);
        RuleFor(x => x.Intensity).InclusiveBetween(0, 100).When(x => x.Intensity.HasValue);
    }
}

public class AdminAuditLogQueryValidator : AbstractValidator<AdminAuditLogQuery>
{
    public AdminAuditLogQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Action).MaximumLength(100).When(x => x.Action is not null);
        RuleFor(x => x.EntityType).MaximumLength(100).When(x => x.EntityType is not null);
    }
}
