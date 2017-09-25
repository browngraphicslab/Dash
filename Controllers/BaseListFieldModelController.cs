using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public abstract class BaseListFieldModelController : FieldModelController<ListFieldModel>
    {
        public abstract List<FieldControllerBase> Data { get; set; }

        protected BaseListFieldModelController(FieldModel fieldModel) : base(fieldModel)
        {
        }

        public override TypeInfo TypeInfo => TypeInfo.List;

        public abstract TypeInfo ListSubTypeInfo { get; }

        public abstract void Add(FieldControllerBase fmc);
        public abstract void AddRange(IList<FieldControllerBase> fmcs);
        /*
        public override bool CheckType(FieldControllerBase fmc)
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
        }*/
    }
}