using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    class ListFieldModel : FieldModel
    {
        public ListFieldModel(IEnumerable<string> l, TypeInfo subTypeInfo)
        {
            Data = new List<string>(l);
            SubTypeInfo = subTypeInfo;
        }

        public List<string> Data;

        public TypeInfo SubTypeInfo;
    }
}
