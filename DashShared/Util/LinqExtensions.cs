using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DashShared
{
    public static class LinqExtensions
    {
        public static int IndexOf<T>(this IEnumerable<T> ienumerable, T item)
        {
            return Array.IndexOf(ienumerable.ToArray(), item);
        }
    }
}
