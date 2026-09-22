public enum ValidationSeverity
{
    Error,
    Warning,
    Info
}

public class ValidationIssue
{
    public ValidationSeverity Severity { get; }
    public string Code { get; }
    public string Message { get; }
    public string SourceGuid { get; }

    public ValidationIssue(ValidationSeverity severity, string code, string message, string sourceGuid)
    {
        Severity = severity;
        Code = code;
        Message = message;
        SourceGuid = sourceGuid;
    }
}
