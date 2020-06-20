using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eveldee._3DSCPlusViGEm.Utils
{
    public sealed class Some<T> : Option<T>
    {
        private readonly T _value;

        public Some(T value)
        {
            _value = value;
        }

        public override void With(Action<T> action)
        {
            action(_value);
        }

        public override R Select<R>(Func<T, R> func, R ifNone)
        {
            return func(_value);
        }

        public override string ToString() => $"Some({_value})";
    }
}
