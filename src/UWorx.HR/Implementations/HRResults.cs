using System.Collections.Generic;
using UWorx.HR.Abstractions;

namespace UWorx.HR.Implementations;

public class HRResult : IHRResult
{
    readonly ICollection<string> errors;

    public HRResult()
    {
        this.errors = new List<string>();
    }

    public ICollection<string> Errors => this.errors;

    public bool Succeeded => Errors.Count == 0;

    public IHRResult AddError(string error) // for fluent api
    {
        this.errors.Add(error);
        return this;
    }
}

public class HRDataResult<T> : HRResult, IHRDataResult<T>
{
    readonly T data;

    public HRDataResult(T data) : base()
    {
        this.data = data;
    }

    public T Data => this.data;
}
