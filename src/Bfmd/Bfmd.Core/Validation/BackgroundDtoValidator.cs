using Bfmd.Core.Domain;
using FluentValidation;

namespace Bfmd.Core.Validation;

public class BackgroundDtoValidator : AbstractValidator<BackgroundDto>
{
    public BackgroundDtoValidator()
    {
        Include(new BaseEntityValidator<BackgroundDto>());
        RuleFor(x => x.SkillProficiencies).Must(HasPicksOrGranted).WithMessage("background must grant skills or have choose/from");
        RuleFor(x => x.TalentOptions.Choose).GreaterThanOrEqualTo(0);
    }

    private static bool HasPicksOrGranted(SkillsPickDto s)
        => s.Granted.Count > 0 || (s.Choose.HasValue && s.Choose.Value >= 0 && s.From.Count > 0);
}

