using Bfmd.Core.Domain;
using FluentValidation;

namespace Bfmd.Core.Validation;

public class ClassDtoValidator : AbstractValidator<ClassDto>
{
    public ClassDtoValidator()
    {
        Include(new BaseEntityValidator<ClassDto>());
        RuleFor(x => x.HitDie).Must(h => new[] { "d6", "d8", "d10", "d12" }.Contains(h))
            .WithMessage("hitDie must be one of d6,d8,d10,d12");
        RuleFor(x => x.SavingThrows).Must(s => s.Distinct(StringComparer.OrdinalIgnoreCase).Count() == 2)
            .WithMessage("savingThrows must contain exactly two unique values");
        RuleFor(x => x.Levels).Custom((levels, ctx) =>
        {
            if (levels.Count != 20) { ctx.AddFailure("levels", "levels must contain 20 rows (1..20)"); return; }
            for (var i = 0; i < 20; i++)
            {
                var lvl = levels[i];
                if (lvl.Level != i + 1) ctx.AddFailure($"levels[{i}]", "levels must be continuous 1..20");
                if (string.IsNullOrWhiteSpace(lvl.ProficiencyBonus)) ctx.AddFailure($"levels[{i}].proficiencyBonus", "required");
                lvl.Features ??= [];
                if (lvl.SpellSlots != null)
                {
                    foreach (var k in lvl.SpellSlots.Keys)
                        if (k < 1 || k > 9) ctx.AddFailure($"levels[{i}].spellSlots", "spell slot keys must be 1..9");
                }
            }
        });
    }
}

