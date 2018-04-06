using System;
using DashShared;

namespace Dash
{
    public interface IQuery<T> : ISerializable where T:EntityBase
    {
        Func<T, bool> Func { get; }
    }
}
