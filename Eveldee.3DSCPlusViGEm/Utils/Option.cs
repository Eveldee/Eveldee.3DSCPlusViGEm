using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eveldee._3DSCPlusViGEm.Utils
{
    public abstract class Option<T>
    {
        public static implicit operator Option<T>(T value) => new Some<T>(value);

        public static implicit operator Option<T>(None _) => new None<T>();

        public static Option<T> Of(T value)
        {
            return new Some<T>(value);
        }

        public static None<T> None()
        {
            return new None<T>();
        }

        public abstract void With(Action<T> action);
        public abstract R Select<R>(Func<T, R> func, R ifNone);
    }
}
