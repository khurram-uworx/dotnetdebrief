using System.Collections.Generic;
using UWorx.HR.Abstractions;

namespace UWorx.HR.Implementations
{
    public class HRResult : IHRResult
    {
        protected readonly ICollection<string> _errors;

        public HRResult()
        {
            _errors = new List<string>();
        }

        public ICollection<string> Errors => _errors;

        public bool Succeeded => Errors.Count == 0;

        public IHRResult AddError(string error) // for fluent api
        {
            _errors.Add(error);
            return this;
        }
    }

    public class HRDataResult<T> : HRResult, IHRDataResult<T>
    {
        protected readonly T _data;
        
        public HRDataResult(T data) : base()
        {
            _data = data;
        }

        public T Data => _data;
    }
}
