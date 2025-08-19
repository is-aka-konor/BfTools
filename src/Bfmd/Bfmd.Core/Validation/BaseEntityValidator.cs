using Bfmd.Core.Domain;
using FluentValidation;

namespace Bfmd.Core.Validation;

public class BaseEntityValidator<T> : AbstractValidator<T> where T : BaseEntity
{
    public BaseEntityValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Slug).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Type).NotEmpty();
        RuleFor(x => x.SchemaVersion).NotEmpty();
        RuleFor(x => x.Src).SetValidator(new SourceRefValidator());
    }
}

