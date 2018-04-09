﻿using System.Collections.Generic;
using DashShared;

namespace Dash
{
    [FieldModelTypeAttribute(TypeInfo.List)]
    public class ListModel : FieldModel
    {
        public ListModel() : base(null)
        {
        }

        public ListModel(IEnumerable<string> l, TypeInfo subTypeInfo, string id = null) : base(id)
        {
            Data = new List<string>(l);
            SubTypeInfo = subTypeInfo;

            if (SubTypeInfo.Equals(TypeInfo.None))
            {

            }
        }

        public List<string> Data= new List<string>();

        public TypeInfo SubTypeInfo;
    }
}
