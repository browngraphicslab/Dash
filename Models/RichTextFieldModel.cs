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

        public RichTextFieldModel()
        {
            
        }

        public RichTextFieldModel(RTD data)
        {
            Data = data;
        }

        protected override FieldModelDTO GetFieldDTOHelper()
        {
            throw new NotImplementedException();
        }
    }
}
