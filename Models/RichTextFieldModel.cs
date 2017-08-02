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
        public string Data;

        public RichTextFieldModel()
        {
            
        }

        public RichTextFieldModel(string data)
        {
            Data = data;
        }

        protected override FieldModelDTO GetFieldDTOHelper()
        {
            throw new NotImplementedException();
        }
    }
}
