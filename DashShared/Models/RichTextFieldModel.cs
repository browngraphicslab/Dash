using DashShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DashShared
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
            return new FieldModelDTO(TypeInfo.RichText, Data);
        }

    }
}
