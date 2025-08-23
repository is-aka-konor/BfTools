using BfCommon.Domain.Models;
using BfSiteGen.Core.Validation;

namespace BfSiteGen.Core.IO;

public sealed class ContentLoadResult
{
    public List<ValidationError> Errors { get; } = new();
    public List<SpellDto> Spells { get; } = new();
    public List<TalentDto> Talents { get; } = new();
    public List<BackgroundDto> Backgrounds { get; } = new();
    public List<ClassDto> Classes { get; } = new();
    public List<LineageDto> Lineages { get; } = new();
}
