using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class FunctionOperatorModel : OperatorModel
    {
        public string FunctionCode { get; set; }

        public FunctionOperatorModel(string functionCode, KeyModel type, string id = null) : base(type, id)
        {
            FunctionCode = functionCode;
        }
    }
}
