using Bfmd.Core.Domain;
using FluentValidation;

namespace Bfmd.Core.Validation;

public class SourceRefValidator : AbstractValidator<SourceRef>
{
    public SourceRefValidator()
    {
        RuleFor(x => x.Abbr).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Version).NotEmpty();
        RuleFor(x => x.Hash).NotEmpty();
    }
}

