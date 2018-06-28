using System.Collections.Generic;
using System.Diagnostics;
using DashShared;

namespace Dash
{
    public abstract class BaseListController : FieldModelController<ListModel>
    {
        public abstract List<FieldControllerBase> Data { get; set; }

        protected BaseListController(ListModel fieldModel) : base(fieldModel)
        {
        }

        public override TypeInfo TypeInfo => TypeInfo.List;

        public abstract TypeInfo ListSubTypeInfo { get; }

        public abstract void Remove(FieldControllerBase fmc);
        public abstract void AddBase(FieldControllerBase fmc);
        public abstract void AddRange(IList<FieldControllerBase> fmcs);

        public abstract void SetValue(int index, FieldControllerBase field);
        public abstract FieldControllerBase GetValue(int index);

        public int Count => Data.Count;
        
        public override bool CheckType(FieldControllerBase fmc)
        {
            bool isList = base.CheckType(fmc);
            if (isList)
            {
                var list = fmc as BaseListController;
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