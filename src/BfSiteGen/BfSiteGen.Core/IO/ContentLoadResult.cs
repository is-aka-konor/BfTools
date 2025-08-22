using BfSiteGen.Core.Models;
using BfSiteGen.Core.Validation;

namespace BfSiteGen.Core.IO;

public sealed class ContentLoadResult
{
    public List<ValidationError> Errors { get; } = new();
    public List<Spell> Spells { get; } = new();
    public List<Talent> Talents { get; } = new();
    public List<Background> Backgrounds { get; } = new();
    public List<Class> Classes { get; } = new();
    public List<Lineage> Lineages { get; } = new();
}
