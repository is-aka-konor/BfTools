namespace Bfmd.Core.Validation;

public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<(string PropertyName, string ErrorMessage)> Errors { get; } = new();
    public void Add(string property, string message) => Errors.Add((property, message));
}

