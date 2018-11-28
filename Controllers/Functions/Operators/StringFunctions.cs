using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public static class StringFunctions
    {
        [OperatorReturnName("Split")]
        public static ListController<TextController> Split(TextController field, TextController delimiter)
        {
            var ret = new ListController<TextController>();
            foreach (var s in field.Data.Split(delimiter.Data))
            {
                ret.Add(new TextController(s));
            }

            return ret;
        }
    }
}
