using Bfmd.Core.Domain;
using FluentValidation;

namespace Bfmd.Core.Validation;

public class LineageDtoValidator : AbstractValidator<LineageDto>
{
    public LineageDtoValidator()
    {
        Include(new BaseEntityValidator<LineageDto>());
        RuleFor(x => x.Size).NotEmpty();
        RuleFor(x => x.Speed).GreaterThan(0);
        RuleFor(x => x.Traits).NotEmpty();
    }
}

