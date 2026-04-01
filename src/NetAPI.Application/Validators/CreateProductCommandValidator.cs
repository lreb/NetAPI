using FluentValidation;
using NetAPI.Application.Commands;

namespace NetAPI.Application.Validators;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private static readonly string[] ValidCurrencies = ["USD", "EUR", "GBP", "BRL", "JPY"];

    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.");

        RuleFor(x => x.Currency)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.")
            .Must(c => ValidCurrencies.Contains(c.ToUpperInvariant()))
            .WithMessage($"Currency must be one of: {string.Join(", ", ValidCurrencies)}");

        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0).WithMessage("Initial stock cannot be negative.");
    }
}
