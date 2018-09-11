using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    [FieldModelType(TypeInfo.Html)]
    class HtmlModel : FieldModel
    {

        public HtmlModel(string html, string id = null) : base(id)
        {
            Data = html;
        }

        public string Data;
    }
}
