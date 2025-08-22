namespace BfSiteGen.Core.Validation;

public sealed class ValidationError
{
    public ValidationError(string category, string filePath, string slug, string field, string message)
    {
        Category = category;
        FilePath = filePath;
        Slug = slug;
        Field = field;
        Message = message;
    }

    public string Category { get; }
    public string FilePath { get; }
    public string Slug { get; }
    public string Field { get; }
    public string Message { get; }
}

