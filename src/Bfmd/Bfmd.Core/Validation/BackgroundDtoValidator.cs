using Bfmd.Core.Domain;

namespace Bfmd.Core.Validation;

public class BackgroundDtoValidator
{
    public ValidationResult Validate(BackgroundDto b)
    {
        var r = new ValidationResult();
        BaseEntityValidator.Validate(b, r);
        if (!(b.SkillProficiencies.Granted.Count > 0 || b.SkillProficiencies is { Choose: >= 0, From.Count: > 0 }))
            r.Add("skillProficiencies", "must grant skills or have choose/from");
        if (b.TalentOptions.Choose < 0) r.Add("talentOptions.choose", ">= 0");
        return r;
    }
}
