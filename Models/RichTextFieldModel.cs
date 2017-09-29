using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;

namespace Dash
{
    /// <summary>
    /// A Field Model which holds rich text data
    /// </summary>
    [FieldModelTypeAttribute(TypeInfo.RichTextField)]
    public class RichTextFieldModel:FieldModel
    {
        public class RTD {
            public string RtfFormatString { get; set; }
            public string ReadableString { get; set; }

            // default constructor for json deserialization
            public RTD()
            {
                
            }

            public RTD(string readableString) { ReadableString = readableString; RtfFormatString = readableString;  }
            public RTD(string readableString, string rtfstring) { ReadableString = readableString; RtfFormatString = rtfstring;  }
        }

        public RTD Data;

        public RichTextFieldModel(RTD data = null, string id = null) : base(id)
        {
            Data = data ?? new RTD("");
        }
    }
}
