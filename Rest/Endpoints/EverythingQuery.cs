using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class EverythingQuery<T> : IQuery<T> where T:EntityBase
    {
        public Func<T, bool> Func => (i) => true;
    }
}
