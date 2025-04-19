using System.Collections.Generic;

namespace UWorx.HR.Abstractions;

public interface IHRResult
{
    ICollection<string> Errors { get; }
    bool Succeeded { get; }
    IHRResult AddError(string error); // for fluent api
}

public interface IHRDataResult<T> : IHRResult
{
    T Data { get; }
}
