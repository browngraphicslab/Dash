using System;
using DashShared;

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
