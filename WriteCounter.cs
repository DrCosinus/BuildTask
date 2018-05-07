using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildTask
{
    internal struct WriteCounter<T>
    {
        public bool HasValue { get => writing_count_ > 0; }
        public bool WasCrashed { get => writing_count_ > 1; }
        public T Value { get => value_; set { SetValue(value); } }
        public uint WritingCount => writing_count_;
        private uint writing_count_;
        private T value_;

        public T GetValueOrDefault(T _default_value) => HasValue ? Value : _default_value;
        private void SetValue(T _value) { value_ = _value; writing_count_++; }
        public override string ToString()
        {
            return $"{(HasValue? (WasCrashed ? $"!CRASHED! {value_}" : $"{value_}") : "(unset)")}";
        }
    }
}
