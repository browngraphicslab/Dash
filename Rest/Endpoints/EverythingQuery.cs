using System;
using DashShared;

namespace Dash
{
    public class EverythingQuery<T> : IQuery<T> where T:EntityBase
    {
        public Func<T, bool> Func => (i) => true;
    }
}
