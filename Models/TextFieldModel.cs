using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    class TextFieldModel : FieldModel
    {
        public TextFieldModel()
        {
            
        }

        public TextFieldModel(string data)
        {
            Data = data;
        }

        public string Data { get; set; }
    }
}
