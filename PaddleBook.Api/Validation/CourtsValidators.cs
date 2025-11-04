using FluentValidation;
using PaddleBook.Api.Contracts;

public class CreateCourtValidator : AbstractValidator<CreateCourtDto>
{
    public CreateCourtValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100);

        RuleFor(x => x.Surface)
            .NotEmpty().WithMessage("Surface is required")
            .Must(s => AllowedSurfaces.Contains(s.ToLower()))
            .WithMessage($"Surface must be one of: {string.Join(", ", AllowedSurfaces)}");
    }

    private static readonly string[] AllowedSurfaces = ["resina", "cemento", "tierra"];
}

public class UpdateCourtValidator : AbstractValidator<UpdateCourtDto>
{
    public UpdateCourtValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100);

        RuleFor(x => x.Surface)
            .NotEmpty().WithMessage("Surface is required")
            .Must(s => AllowedSurfaces.Contains(s.ToLower()))
            .WithMessage($"Surface must be one of: {string.Join(", ", AllowedSurfaces)}");
    }

    private static readonly string[] AllowedSurfaces = ["resina", "cemento", "tierra"];
}


// pequeño helper para reutilizar reglas entre DTOs equivalentes
public static class ValidatorExtensions
{
    public static IValidator<TTo> ConvertToChildValidator<TTo>(this IValidator validator)
        => new DelegatingValidator<TTo>(o => validator.Validate(new ValidationContext<object>(o!)));

    private class DelegatingValidator<T> : AbstractValidator<T>
    {
        public DelegatingValidator(Func<T, FluentValidation.Results.ValidationResult> f)
        {
            RuleFor(x => x).Custom((obj, ctx) =>
            {
                var r = f(obj!);
                foreach (var e in r.Errors)
                    ctx.AddFailure(e.PropertyName, e.ErrorMessage);
            });
        }
    }
}
