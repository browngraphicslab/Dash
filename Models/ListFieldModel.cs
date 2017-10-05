using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;

namespace Dash
{
    [FieldModelTypeAttribute(TypeInfo.List)]
    public class ListFieldModel : FieldModel
    {
        public ListFieldModel() : base(null)
        {
        }

        public ListFieldModel(IEnumerable<string> l, TypeInfo subTypeInfo, string id = null) : base(id)
        {
            Data = new List<string>(l);
            SubTypeInfo = subTypeInfo;
        }

        public List<string> Data;

        public TypeInfo SubTypeInfo;
    }
}
