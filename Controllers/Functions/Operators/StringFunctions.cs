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
            return field.Data.Split(delimiter.Data).Select(s => new TextController(s)).ToListController();
        }
    }
}
