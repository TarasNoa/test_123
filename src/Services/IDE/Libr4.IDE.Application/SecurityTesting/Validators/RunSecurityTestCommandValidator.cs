/*
using FluentValidation;
using Libr4.IDE.Application.SecurityTesting.Commands;

namespace Libr4.IDE.Application.SecurityTesting.Validators;

public class RunSecurityTestCommandValidator : AbstractValidator<RunSecurityTestCommand>
{
    public RunSecurityTestCommandValidator()
    {
        RuleFor(x => x.Target)
            .NotEmpty().WithMessage("Target is required")
            .Must(BeValidUrlOrPath).WithMessage("Target must be a valid URL or file path");

        RuleFor(x => x.TestTypes)
            .NotEmpty().WithMessage("At least one test type must be specified")
            .Must(BeValidTestTypes).WithMessage("Invalid test types specified");

        RuleFor(x => x.Timeout)
            .GreaterThan(0).WithMessage("Timeout must be greater than 0")
            .LessThanOrEqualTo(TimeSpan.FromHours(1)).WithMessage("Timeout must be less than 1 hour");
    }

    private bool BeValidUrlOrPath(string target)
    {
        return Uri.TryCreate(target, UriKind.Absolute, out var uriResult) && 
               (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps) ||
               System.IO.Path.IsPathRooted(target);
    }

    private bool BeValidTestTypes(System.Collections.Generic.IEnumerable<string> testTypes)
    {
        var validTypes = new[] { "sql_injection", "xss", "csrf", "auth", "config", "dependency" };
        return testTypes.All(t => validTypes.Contains(t.ToLowerInvariant()));
    }
}
*/
