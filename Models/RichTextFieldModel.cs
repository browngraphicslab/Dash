using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    /// <summary>
    /// A Field Model which holds rich text data
    /// </summary>
    public class RichTextFieldModel:FieldModel
    {
        public class RTD {
            public string RtfFormatString;
            public string ReadableString;
            public RTD(string readableString) { ReadableString = readableString; RtfFormatString = readableString;  }
            public RTD(string readableString, string rtfstring) { ReadableString = readableString; RtfFormatString = rtfstring;  }
        }

        public RTD Data;

        public RichTextFieldModel(RTD data = null, string id = null) : base(id)
        {
            Data = data ?? new RTD("");
        }

        protected override FieldModelDTO GetFieldDTOHelper()
        {
            return new FieldModelDTO(TypeInfo.RichTextField, Data, Id);
        }
    }
}
