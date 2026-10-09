namespace VoidRemove.Core.Primitives;

/// <summary>
/// Outcome of an operation that can fail without it being exceptional.
/// </summary>
/// <remarks>
/// A struct, so the happy path of a scan over thousands of leftovers does not allocate.
/// </remarks>
public readonly record struct Result(bool Success, string? Error = null, Exception? Exception = null)
{
    /// <summary>True when the operation did not succeed.</summary>
    public bool IsFailure => !Success;

    /// <summary>Successful outcome.</summary>
    public static Result Ok() => new(true);

    /// <summary>Failed outcome with a message the user can read.</summary>
    public static Result Fail(string error, Exception? exception = null) => new(false, error, exception);

    /// <inheritdoc />
    public override string ToString() => Success ? "Ok" : $"Fail: {Error}";
}

/// <summary><see cref="Result"/> carrying a value.</summary>
public readonly record struct Result<T>(bool Success, T? Value = default, string? Error = null, Exception? Exception = null)
{
    /// <summary>True when the operation did not succeed.</summary>
    public bool IsFailure => !Success;

    /// <summary>Successful outcome.</summary>
    public static Result<T> Ok(T value) => new(true, value);

    /// <summary>Failed outcome with a message the user can read.</summary>
    public static Result<T> Fail(string error, Exception? exception = null) => new(false, default, error, exception);

    /// <summary>Converts a value-less result into a value-bearing one.</summary>
    public static implicit operator Result<T>(Result result) =>
        result.Success ? new Result<T>(true) : new Result<T>(false, default, result.Error, result.Exception);

    /// <inheritdoc />
    public override string ToString() => Success ? $"Ok({Value})" : $"Fail: {Error}";
}
