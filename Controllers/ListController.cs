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
    public class ListController<T> : BaseListController where T : FieldControllerBase
    {
        private List<T> _typedData = new List<T>();

        /// <summary>
        /// Wrapper to retrieve the list items stored in the ListController.
        /// </summary>
        public List<T> TypedData
        {
            get { return _typedData; }
            set
            {
                if (_typedData != null)
                {
                    if (_typedData != value)
                    {
                        foreach (var d in _typedData)
                        {
                            d.FieldModelUpdated -= ContainedFieldUpdated;
                        }
                        foreach (var d in value)
                        {
                            d.FieldModelUpdated += ContainedFieldUpdated;
                        }
                        _typedData = value;
                        OnFieldModelUpdated(null);

                        UpdateOnServer();
                    }
                }
                _typedData = value;
            }
        }

        private void ContainedFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            //var dargs = args as DocumentController.DocumentFieldUpdatedEventArgs;
            //if (dargs != null)
            //{
            //    var fieldKey = dargs.Reference.FieldKey;
            //    if (fieldKey.Equals(KeyStore.TitleKey) ||
            //        fieldKey.Equals(KeyStore.PositionFieldKey))
            //    {
            //        OnFieldModelUpdated(null, context);
            //    }
            //}
            //var keylist = (sender
            //    .GetDereferencedField<ListFieldModelController<TextFieldModelController>>(KeyStore.PrimaryKeyKey,
            //        new Context(sender))?.Data.Select((d) => (d as TextFieldModelController).Data));
            //if (keylist != null && keylist.Contains(args.Reference.FieldKey.Id))
            //    OnFieldModelUpdated(args.FieldArgs);
        }
        
        public override object GetValue(Context context)
        {
            return TypedData.ToList();
        }

        public override bool SetValue(object value)
        {
            var list = value as List<T>;
            if (list != null)
            {
                TypedData = list;
                return true;
            }
            return false;
        }

        public ListModel ListModel { get { return Model as ListModel; } }

        public override List<FieldControllerBase> Data
        {
            get { return TypedData.Cast<FieldControllerBase>().ToList(); }
            set { TypedData = value.Cast<T>().ToList(); }
        }

        public ListController(ListModel model) : base(model)
        {

        }

        public ListController() : base(new ListModel(new List<string>(), TypeInfoHelper.TypeToTypeInfo(typeof(T))))
        {
            Init();
        }

        public ListController(IEnumerable<T> list) : base(new ListModel(list.Select(fmc => fmc.GetId()), TypeInfoHelper.TypeToTypeInfo(typeof(T))))
        {
            Init();
        }

        public ListController(T item) : base(new ListModel(new List<T> { item }.Select(fmc => fmc.GetId()), TypeInfoHelper.TypeToTypeInfo(typeof(T))))
        {
            Init();
        }

        public override void Init()
        {
            TypedData = ContentController<FieldModel>.GetControllers<T>(ListModel.Data).ToList();
            UpdateOnServer();
            Debug.Assert(TypeInfoHelper.TypeToTypeInfo(typeof(T)) == ListModel.SubTypeInfo);
        }

        private bool AddHelper(T element)
        {
            if (TypedData.Contains(element))
                return false;
            element.FieldModelUpdated += ContainedFieldUpdated;
            //TODO tfs: Remove deleted fields from the list if we can delete fields 
            TypedData.Add(element);
            ListFieldModel.Data.Add(element.GetId());
            return true;
        }

        private bool RemoveHelper(T element)
        {
            element.FieldModelUpdated -= ContainedFieldUpdated;
            bool removed = TypedData.Remove(element);
            ListFieldModel.Data.Remove(element.GetId());
            return removed;
        }

        public void Add(T element)
        {
            if (AddHelper(element))
            {
                UpdateOnServer();

                OnFieldModelUpdated(new ListFieldUpdatedEventArgs(
                    ListFieldUpdatedEventArgs.ListChangedAction.Add,
                    new List<T> { element }));
            }
        }

        public void AddRange(IList<T> elements)
        {
            foreach (var element in elements)
            {
                AddHelper(element);
                //TODO tfs: Remove deleted fields from the list when they are deleted if we can delete fields 
            }
            UpdateOnServer();

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Add,
                elements.ToList()));
        }

        public void Remove(T element)
        {
            bool removed = RemoveHelper(element);
            if (removed)
            {
                UpdateOnServer();

                OnFieldModelUpdated(new ListFieldUpdatedEventArgs(
                    ListFieldUpdatedEventArgs.ListChangedAction.Remove,
                    new List<T> {element}));
            }
        }

        public void Set(IEnumerable<T> elements)
        {
            foreach (var element in TypedData)
            {
                RemoveHelper(element);
            }
            var enumerable = elements as List<T> ?? elements.ToList();
            foreach (var element in enumerable)
            {
                AddHelper(element);
            }
            UpdateOnServer();

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(
                ListFieldUpdatedEventArgs.ListChangedAction.Replace,
                enumerable));
        }

        public List<T> GetElements()
        {
            return TypedData.ToList();
        }

        public ListModel ListFieldModel => Model as ListModel;

        public override TypeInfo ListSubTypeInfo { get; } = TypeInfoHelper.TypeToTypeInfo(typeof(T));

        public override void Add(FieldControllerBase fmc)
        {
            if (fmc is T)
            {
                Add((T)fmc);
            }
        }

        public override void AddRange(IList<FieldControllerBase> fmcs)
        {
            if (fmcs is IList<T>)
            {
                AddRange((IList<T>)fmcs);
            }
        }

        public override FrameworkElement GetTableCellView(Context context)
        {
            return GetTableCellViewForCollectionAndLists("📜", delegate (TextBlock block)
            {
                block.Text = string.Format("{0} object(s)", TypedData.Count());           //TODO make a factory and specify what objects it contains ,,,, 
            });
        }

        public override FieldModelController<ListModel> Copy()
        {
            return new ListController<T>(new List<T>(TypedData));
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ListController<T>();
        }

        public override void UpdateOnServer(Action<FieldModel> success = null, Action<Exception> error = null)
        {
            base.UpdateOnServer(success, error);
            foreach (var fmc in TypedData)
            {
                fmc.UpdateOnServer();
            }

            /*
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
            }*/
        }

        public override void SaveOnServer(Action<FieldModel> success = null, Action<Exception> error = null)
        {
            base.SaveOnServer(success, error);
            foreach (var fmc in TypedData)
            {
                fmc.SaveOnServer();
            }
        }

        /// <summary>
        /// recurs on all list items
        /// </summary>
        /// <param name="searchString"></param>
        /// <returns></returns>
        public override StringSearchModel SearchForString(string searchString)
        {
            return TypedData.FirstOrDefault(controller => controller.SearchForString(searchString).StringFound)?.SearchForString(searchString) ?? StringSearchModel.False;
        }


        /// <summary>
        /// Provides data about how the list changed. Similar to NotifyCollectionChangedEventArgs.
        /// </summary>
        public class ListFieldUpdatedEventArgs : FieldUpdatedEventArgs
        {
            public enum ListChangedAction
            {
                Add,
                Remove,
                Replace,
                Clear
            }

            public readonly List<T> ChangedDocuments;

            public readonly ListChangedAction ListAction;

            private ListFieldUpdatedEventArgs() : base(TypeInfo.List,
                DocumentController.FieldUpdatedAction.Update)
            {
            }

            public ListFieldUpdatedEventArgs(ListChangedAction action) : this()
            {
                if (action != ListChangedAction.Clear)
                    throw new ArgumentException();
                ListAction = action;
                ChangedDocuments = null;
            }

            public ListFieldUpdatedEventArgs(ListChangedAction action,
                List<T> changedDocuments) : this()
            {
                ListAction = action;
                ChangedDocuments = changedDocuments;
            }
        }

        // todo: replace with better value override
        public override string ToString()
        {
            return "Items";
        }
        // override ToString() to get displayable string representation of field
        public override string GetTypeAsString()
        {
            if (ListModel.SubTypeInfo == TypeInfo.Document)
                return "List:Doc"; // uses truncated 'doc' instead of 'document'
            else
                return "List:" + ListModel.SubTypeInfo.ToString();
        }
    }

}