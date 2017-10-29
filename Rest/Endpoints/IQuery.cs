using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public interface IQuery<T> : ISerializable where T:EntityBase
    {
        Func<T, bool> Func { get; }
    }
}
