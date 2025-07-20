using System;

namespace UWorx.HR.Abstractions;

readonly struct Result<T>
{
    enum ResultState { Null, Failure, Success }
    readonly ResultState state;

    public T Value { get; }
    public Exception Exception { get; }

    public bool IsSuccess => this.state == ResultState.Success;
    public bool IsFailure => this.state == ResultState.Failure;
    public bool IsNull => this.state == ResultState.Null;

    public Result(T value)
    {
        this.Value = value;
        this.Exception = null!;
        this.state = ResultState.Success;
    }

    public Result(Exception exception)
    {
        this.Value = default!;
        this.Exception = exception;
        this.state = ResultState.Failure;
    }

    public Result()
    {
        this.Value = default!;
        this.Exception = null!;
        this.state = ResultState.Null;
    }

    public TR Match<TR>(Func<T, TR> onSuccess, Func<Exception, TR> onFailure, Func<TR>? onNull = null) =>
        IsSuccess ? onSuccess(Value) :
        IsFailure ? onFailure(Exception) :
        onNull is not null
            ? onNull()
            : throw new InvalidOperationException("Result is null, but no onNull function was provided.");

    public static implicit operator Result<T>(T? value) => value is not null ? new Result<T>(value) : new Result<T>();

    /// <summary>
    /// Implicitly converts an Exception to a failure Result.
    /// <para>
    /// <b>Warning:</b> This conversion is convenient, but may hide errors if you do not check <see cref="IsFailure"/>
    /// or use <see cref="Match"/> to handle the failure case. Accessing <see cref="Value"/> when the result is a failure
    /// will return the default value of <typeparamref name="T"/>, which may not be what you expect.
    /// </para>
    /// <code>
    /// var result = GetNumber(true);
    /// // Careless usage: assumes success
    /// int value = result.Value; // This will be default(int) (i.e., 0), not 42!
    /// // The exception is stored, but unless you check IsFailure or use Match, you may miss it.
    /// </code>
    /// </summary>
    public static implicit operator Result<T>(Exception exception) => new(exception);
}
