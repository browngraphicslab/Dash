using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;
using DashShared.Models;

namespace Dash
{
    public class ListFieldModelController<T> : BaseListFieldModelController where T : FieldControllerBase
    {
        public List<T> TypedData { get; set; } = new List<T>();

        public override object GetValue(Context context)
        {
            return Data;
        }

        public override bool SetValue(object value)
        {
            if (value is List<FieldControllerBase>)
            {
                Data = value as List<FieldControllerBase>;
                return true;
            }
            return false;
        }

        public ListFieldModel ListModel { get { return Model as ListFieldModel; } }

        public override List<FieldControllerBase> Data
        {
            get { return TypedData.Cast<FieldControllerBase>().ToList(); }
            set { TypedData = value.Cast<T>().ToList(); }
        }

        public ListFieldModelController(ListFieldModel model) : base(model)
        {
            
        }

        public ListFieldModelController() : base(new ListFieldModel(new List<string>(), TypeInfoHelper.TypeToTypeInfo(typeof(T))))
        {
            Init();
        }

        public ListFieldModelController(IEnumerable<T> list) : base(new ListFieldModel(list.Select(fmc => fmc.GetId()), TypeInfoHelper.TypeToTypeInfo(typeof(T))))
        {
            Init();
        }

        public override void Init()
        {
            TypedData = ContentController<FieldModel>.GetControllers<T>(ListModel.Data).ToList();
            UpdateOnServer();
            Debug.Assert(TypeInfoHelper.TypeToTypeInfo(typeof(T)) == ListModel.SubTypeInfo);
        }

        public void Add(T element)
        {
            TypedData.Add(element);
            ListFieldModel.Data.Add(element.GetId());
            UpdateOnServer();
        }

        public void AddRange(IList<T> elements)
        {
            TypedData.AddRange(elements);
            ListFieldModel.Data.AddRange(elements.Select(fmc => fmc.GetId()));
            UpdateOnServer();
        }

        public void Remove(T element)
        {
            TypedData.Remove(element);
            ListFieldModel.Data.Remove(element.GetId());
            UpdateOnServer();
        }

        public void Set(IEnumerable<T> elements)
        {
            TypedData.Clear();
            var collection = elements as IList<T> ?? elements.ToList();
            TypedData.AddRange(collection);
            ListFieldModel.Data.Clear();
            ListFieldModel.Data.AddRange(collection.Select(e => e.GetId()));
            UpdateOnServer();
        }

        public List<T> GetElements()
        {
            return TypedData.ToList();
        }

        public ListFieldModel ListFieldModel => Model as ListFieldModel;

        public override TypeInfo ListSubTypeInfo { get; } = TypeInfoHelper.TypeToTypeInfo(typeof(T));

        public override void Add(FieldControllerBase fmc)
        {
            Add(fmc as T);
        }

        public override void AddRange(IList<FieldControllerBase> fmcs)
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

        public override FieldModelController<ListFieldModel> Copy()
        {
            return new ListFieldModelController<T>(new List<T>(TypedData));
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ListFieldModelController<T>();
        }

        public override void UpdateOnServer(Action<FieldModel> success = null, Action<Exception> error = null)
        {
            base.UpdateOnServer(success, error);
            foreach (var fmc in TypedData)
            {
                fmc.UpdateOnServer();
            }

            foreach (var fmc in TypedData)
            {
                RESTClient.Instance.GetEndpoint<FieldModel>().GetDocument(fmc.Id,
                    async args =>
                    {
                        Debug.Assert(args.ReturnedObjects.Count() > 0);
                    },
                    exception =>
                    {
                        
                    });
            }
        }

        public override void SaveOnServer(Action<FieldModel> success = null, Action<Exception> error = null)
        {
            base.SaveOnServer(success, error);
            foreach (var fmc in TypedData)
            {
                fmc.SaveOnServer();
            }
        }
    }
}