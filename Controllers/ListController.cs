using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

namespace Dash
{
    public static class ListContainedFieldFlag
    {
        public static bool Enabled = false;
    }

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
                SetTypedData(value);
            }
        }

        /*
         * Sets the data property and gives UpdateOnServer an UndoCommand 
         */
        private void SetTypedData(List<T> val, bool withUndo = true)
        {
            if (_typedData != null)
            {
                if (_typedData != val)
                {
                    List<T> data = _typedData;
                    UndoCommand newEvent = new UndoCommand(() => SetTypedData(val, false), () => SetTypedData(data, false));

                    foreach (var d in _typedData)
                    {
                        d.FieldModelUpdated -= ContainedFieldUpdated;
                    }
                    foreach (var d in val)
                    {
                        d.FieldModelUpdated += ContainedFieldUpdated;
                    }
                    _typedData = val;

                    UpdateOnServer(withUndo ? newEvent : null);
                    OnFieldModelUpdated(null);

                }
            }
            _typedData = val;
        }


        private void ContainedFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            if (ListContainedFieldFlag.Enabled)
            {
                var dargs = args as DocumentController.DocumentFieldUpdatedEventArgs;
                if (dargs != null)
                {
                    Debug.Assert(sender is T);
                    var fieldKey = dargs.Reference.FieldKey;
                    if (fieldKey.Equals(KeyStore.TitleKey) || fieldKey.Equals(KeyStore.PositionFieldKey) || fieldKey.Equals(KeyStore.HiddenKey))
                    {
                        OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Content, new List<T> { (T)sender }), context);
                    }
                }
            }
        }
        
        public override object GetValue(Context context)
        {
            return TypedData.ToList();
        }

        public override bool TrySetValue(object value)
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
            SaveOnServer();
            Init();
        }

        public ListController(IEnumerable<T> list) : base(new ListModel(list.Select(fmc => fmc?.GetId()), TypeInfoHelper.TypeToTypeInfo(typeof(T))))
        {
            SaveOnServer();
            Init();
        }

        public ListController(T item) : base(new ListModel(new List<T> { item }.Select(fmc => fmc.GetId()), TypeInfoHelper.TypeToTypeInfo(typeof(T))))
        {
            SaveOnServer();
            Init();
        }

        public override void Init()
        {
            //why have a list of none?
            Debug.Assert(!(Model as ListModel).SubTypeInfo.Equals(TypeInfo.None));
            TypedData = ContentController<FieldModel>.GetControllers<T>(ListModel.Data).ToList();
            Debug.Assert(TypeInfoHelper.TypeToTypeInfo(typeof(T)) == ListModel.SubTypeInfo);
        }

        private bool AddHelper(T element, int where = -1)
        {
            if (TypedData.Contains(element))
                return false;
            element.FieldModelUpdated += ContainedFieldUpdated;
            //TODO tfs: Remove deleted fields from the list if we can delete fields 
            if (where == -1)
            {
                TypedData.Add(element);
                ListFieldModel.Data.Add(element.GetId());
            }
            else
            {
                TypedData.Insert(where, element);
                ListFieldModel.Data.Insert(where, element.GetId());
            }
            return true;
        }

        private bool RemoveHelper(T element)
        {
            element.FieldModelUpdated -= ContainedFieldUpdated;
            bool removed = TypedData.Remove(element);
            ListFieldModel.Data.Remove(element.GetId());
            return removed;
        }

        public void Add(T element, int where = -1, bool withUndo = true)
        {
            if (AddHelper(element, where))
            {
                UndoCommand newEvent = new UndoCommand(() => Add(element, where, false), () => Remove(element, false));

                UpdateOnServer(withUndo ? newEvent : null);

                OnFieldModelUpdated(new ListFieldUpdatedEventArgs(
                    ListFieldUpdatedEventArgs.ListChangedAction.Add,
                    new List<T> { element }));
            }
        }

        public void AddRange(IList<T> elements, bool withUndo = true)
        {
            foreach (var element in elements)
            {
                AddHelper(element);
                //TODO tfs: Remove deleted elements from the list when they are deleted if we can delete fields 
                // Or just use reference counting if that ever gets implemented
            }

            UndoCommand newEvent = new UndoCommand(() => AddRange(elements, false), () => {
                foreach (var element in elements) {
                    Remove(element, false);
                    } });

            UpdateOnServer(withUndo ? newEvent : null);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Add,
                elements.ToList()));
        }

        public void Remove(T element, bool withUndo = true)
        {
            bool removed = RemoveHelper(element);
            if (removed)
            {
                UndoCommand newEvent = new UndoCommand(() => Remove(element, false), () => Add(element, -1, false));

                UpdateOnServer(withUndo ? newEvent : null);

                OnFieldModelUpdated(new ListFieldUpdatedEventArgs(
                    ListFieldUpdatedEventArgs.ListChangedAction.Remove,
                    new List<T> { element }));
            }
        }

        public void Set(IEnumerable<T> elements, bool withUndo = true)
        {
            //it looks like this function deletes everything in TypedData and replaces it with elements
            IEnumerable<T> oldElements = TypedData;
            UndoCommand newEvent = new UndoCommand(() => Set(elements, false), () => Set(oldElements, false));

            foreach (var element in TypedData)
            {
                RemoveHelper(element);
            }
            var enumerable = elements as List<T> ?? elements.ToList();
            foreach (var element in enumerable)
            {
                AddHelper(element);
            }
            UpdateOnServer(withUndo ? newEvent : null);

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

        public override void Remove(FieldControllerBase fmc)
        {
            if (fmc is T)
            {
                Remove((T)fmc);
            }
        }
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

        public override FieldControllerBase Copy()
        {
            return new ListController<T>(new List<T>(TypedData));
        }

        public override FieldControllerBase GetDefaultController()
        {
            return new ListController<T>();
        }

        public override void UpdateOnServer(UndoCommand undoEvent, Action<FieldModel> success = null, Action<Exception> error = null)
        {
            base.UpdateOnServer(undoEvent, success, error);
            //foreach (var fmc in TypedData)
            //{
            //    fmc.UpdateOnServer();
            //}

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
            //foreach (var fmc in TypedData)
            //{
            //    fmc.SaveOnServer();
            //}
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
                Add, //Items were added to the list
                Remove, //Items were removed from the list
                Replace, //Items in the list were replaced with other items
                Clear, //The list was cleared
                Update, //An item in the list was updated
                Content
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