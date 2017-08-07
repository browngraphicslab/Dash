using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public abstract class BaseListFieldModelController : FieldModelController
    {
        public abstract List<FieldModelController> Data { get; set; }

        protected BaseListFieldModelController(FieldModel fieldModel) : base(fieldModel)
        {
        }

        public override TypeInfo TypeInfo => TypeInfo.List;

        public abstract TypeInfo ListSubTypeInfo { get; }

        public abstract void Add(FieldModelController fmc);
        public abstract void AddRange(IList<FieldModelController> fmcs);

        public override bool CheckType(FieldModelController fmc)
        {
            bool isList = base.CheckType(fmc);
            if (isList)
            {
                var list = fmc as BaseListFieldModelController;
                if (list == null)
                {
                    return false;
                }
                Debug.Assert((list.ListSubTypeInfo & ListSubTypeInfo) != TypeInfo.None);
                return (list.ListSubTypeInfo & ListSubTypeInfo) != TypeInfo.None;
            }
            else
            {
                return false;
            }
        }
    }
}