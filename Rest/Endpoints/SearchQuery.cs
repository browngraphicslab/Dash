using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared.Models;

namespace Dash
{
    public class SearchQuery : IQuery<FieldModel>
    {
        public SearchQuery(Func<FieldModel, bool> func)
        {
            Func = func;
        }
        public Func<FieldModel, bool> Func { get; private set; }
    }
}
