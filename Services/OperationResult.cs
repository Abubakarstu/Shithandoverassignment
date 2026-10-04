namespace ShiftHandover.Services;

/// <summary>
/// Result of a business operation. Keeps controllers free of scattered validation logic.
/// </summary>
public class OperationResult
{
    public bool Succeeded { get; init; }
    public string? ErrorMessage { get; init; }

    public static OperationResult Ok() => new() { Succeeded = true };

    public static OperationResult Fail(string message) => new() { Succeeded = false, ErrorMessage = message };
}

public class OperationResult<T> : OperationResult
{
    public T? Value { get; init; }

    public static OperationResult<T> Ok(T value) => new() { Succeeded = true, Value = value };

    public new static OperationResult<T> Fail(string message) => new() { Succeeded = false, ErrorMessage = message };
}