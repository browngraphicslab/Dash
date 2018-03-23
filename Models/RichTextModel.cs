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
    [FieldModelTypeAttribute(TypeInfo.RichText)]
    public class RichTextModel:FieldModel
    {
        public class RTD {
            public string RtfFormatString { get; set; }

            // default constructor for json deserialization
            public RTD() { }

            public RTD(string rtfFormatString) { RtfFormatString = rtfFormatString;  }
            public override string ToString() { return RtfFormatString; }
        }

        public RTD Data;


        public RichTextModel() { }

        public RichTextModel(RTD data = null, string id = null) : base(id)
        {
            Data = data ?? new RTD("");
        }
    }
}
