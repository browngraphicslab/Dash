using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using DashShared;

namespace Dash
{
    class ListFieldModelController<T> : FieldModelController where T : FieldModelController
    {
        public List<T> Data { get; set; }

        public ListFieldModelController() : base(new ListFieldModel(new List<string>(), TypeInfoHelper.TypeToTypeInfo(typeof(T))))
        {
        }

        public ListFieldModelController(IEnumerable<T> list) : base(new ListFieldModel(list.Select(fmc => fmc.GetId()), TypeInfoHelper.TypeToTypeInfo(typeof(T))))
        {
        }

        public void Add(T element)
        {
            Data.Add(element);
            ListFieldModel.Data.Add(element.GetId());
        }

        public void Remove(T element)
        {
            Data.Remove(element);
            ListFieldModel.Data.Remove(element.GetId());
        }

        public void Set(IEnumerable<T> elements)
        {
            Data.Clear();
            var collection = elements as IList<T> ?? elements.ToList();
            Data.AddRange(collection);
            ListFieldModel.Data.Clear();
            ListFieldModel.Data.AddRange(collection.Select(e => e.GetId()));
        }

        public List<T> GetElements()
        {
            return Data.ToList();
        }

        public ListFieldModel ListFieldModel => FieldModel as ListFieldModel;

        public override TypeInfo TypeInfo => TypeInfo.List;

        public TypeInfo ListSubTypeInfo => TypeInfoHelper.TypeToTypeInfo(typeof(T));

        public override bool CheckType(FieldModelController fmc)
        {
            bool isList = base.CheckType(fmc);
            if (isList)
            {
                var list = fmc as ListFieldModelController<T>;
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

        public override FrameworkElement GetTableCellView()
        {
            throw new NotImplementedException();
        }

        public override FieldModelController GetDefaultController()
        {
            throw new NotImplementedException();
        }
    }
}
