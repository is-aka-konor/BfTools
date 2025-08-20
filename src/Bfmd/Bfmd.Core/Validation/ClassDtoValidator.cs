using Bfmd.Core.Domain;

namespace Bfmd.Core.Validation;

public class ClassDtoValidator
{
    public ValidationResult Validate(ClassDto c)
    {
        var r = new ValidationResult();
        BaseEntityValidator.Validate(c, r);
        if (!new[] { "d6", "d8", "d10", "d12" }.Contains(c.HitDie)) r.Add("hitDie", "must be one of d6,d8,d10,d12");
        if (c.SavingThrows.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 2) r.Add("savingThrows", "must contain exactly two unique values");
        if (c.Levels.Count != 20) r.Add("levels", "must contain 20 rows (1..20)");
        for (var i = 0; i < Math.Min(20, c.Levels.Count); i++)
        {
            var lvl = c.Levels[i];
            if (lvl.Level != i + 1) r.Add($"levels[{i}]", "must be continuous 1..20");
            if (string.IsNullOrWhiteSpace(lvl.ProficiencyBonus)) r.Add($"levels[{i}].proficiencyBonus", "required");
            if (lvl.SpellSlots != null)
            {
                foreach (var k in lvl.SpellSlots.Keys)
                    if (k < 1 || k > 9) r.Add($"levels[{i}].spellSlots", "keys must be 1..9");
            }
        }
        return r;
    }
}

