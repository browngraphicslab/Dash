using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Dash.DocumentController;

namespace Dash
{
    public class FieldUpdatedEventArgs
    {
        public readonly TypeInfo Type;
        public readonly FieldUpdatedAction Action;

        public FieldUpdatedEventArgs(TypeInfo type, FieldUpdatedAction action)
        {
            Type = type;
            Action = action;
        }
    }
}
