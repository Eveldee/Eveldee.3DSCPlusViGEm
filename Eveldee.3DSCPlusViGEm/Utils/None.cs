using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eveldee._3DSCPlusViGEm.Utils
{
    public sealed class None<T> : Option<T>
    {
        public override void With(Action<T> action)
        {
            // Nothing much to do
        }
        public override R Select<R>(Func<T, R> func, R ifNone)
        {
            return ifNone;
        }
    }

    public sealed class None
    {
        public static None Value { get; } = new None();
    }
}
