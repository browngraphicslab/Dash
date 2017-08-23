using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{
    class ListFieldModelController<T> : BaseListFieldModelController where T : FieldModelController
    {
        public List<T> TypedData { get; set; } = new List<T>();

        public override object GetValue()
        {
            return Data;
        }

        public override void SetValue(object value)
        {
            Data = value as List<FieldModelController>;
        }
        public override List<FieldModelController> Data
        {
            get { return TypedData.Cast<FieldModelController>().ToList(); }
            set { TypedData = value.Cast<T>().ToList(); }
        }

        public ListFieldModelController() : base(new ListFieldModel(new List<string>(), TypeInfoHelper.TypeToTypeInfo(typeof(T))))
        {
        }

        public ListFieldModelController(IEnumerable<T> list) : base(new ListFieldModel(list.Select(fmc => fmc.GetId()), TypeInfoHelper.TypeToTypeInfo(typeof(T))))
        {
            TypedData = list.ToList();
        }

        public void Add(T element)
        {
            TypedData.Add(element);
            ListFieldModel.Data.Add(element.GetId());
        }

        public void AddRange(IList<T> elements)
        {
            TypedData.AddRange(elements);
            ListFieldModel.Data.AddRange(elements.Select(fmc => fmc.GetId()));
        }

        public void Remove(T element)
        {
            TypedData.Remove(element);
            ListFieldModel.Data.Remove(element.GetId());
        }

        public void Set(IEnumerable<T> elements)
        {
            TypedData.Clear();
            var collection = elements as IList<T> ?? elements.ToList();
            TypedData.AddRange(collection);
            ListFieldModel.Data.Clear();
            ListFieldModel.Data.AddRange(collection.Select(e => e.GetId()));
        }

        public List<T> GetElements()
        {
            return TypedData.ToList();
        }

        public ListFieldModel ListFieldModel => FieldModel as ListFieldModel;

        public override TypeInfo ListSubTypeInfo { get; } = TypeInfoHelper.TypeToTypeInfo(typeof(T));

        public override void Add(FieldModelController fmc)
        {
            Add(fmc as T);
        }

        public override void AddRange(IList<FieldModelController> fmcs)
        {
            AddRange(fmcs.Cast<T>().ToList());
        }

        public override FrameworkElement GetTableCellView(Context context)
        {
            //return GetTableCellViewOfScrollableText(delegate (TextBlock block)
            //{
            //    block.Text = "[" + string.Join(", ", TypedData) + "]";
            //});
            return GetTableCellViewForCollectionAndLists("📜", delegate (TextBlock block)
            {
                block.Text = string.Format("{0} object(s)", TypedData.Count());           //TODO make a factory and specify what objects it contains ,,,, 
            });
        }

        public override FieldModelController Copy()
        {
            return new ListFieldModelController<T>(new List<T>(TypedData));
        }

        public override FieldModelController GetDefaultController()
        {
            return new ListFieldModelController<T>();
        }
    }
}