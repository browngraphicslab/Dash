using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public static class ListContainedFieldFlag
    {
        public static bool Enabled = false;
    }

    public static class ListExtensions
    {
        public static ListController<T> ToListController<T>(this IEnumerable<T> enumerable) where T : FieldControllerBase
        {
            return new ListController<T>(enumerable);
        }
    }

    public class ListController<T> :FieldModelController<ListModel>, IListController, /*/*INotifyCollectionChanged, */IList<T> where T : FieldControllerBase
    {
        private const bool AvoidDuplicates = false;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public override TypeInfo TypeInfo => TypeInfo.List;


        public FieldControllerBase AsField()
        {
            return this;
        }

        public IEnumerable<FieldControllerBase> AsEnumerable()
        {
            return this;
        }

        #region // DATA //

        //private void OnCollectionChanged(NotifyCollectionChangedEventArgs args) => CollectionChanged?.Invoke(this, args);

        /*
         * Wrapper to retrieve the list items stored in the ListController.
         */
        private List<T> _typedData;

        public bool IsEmpty => Count == 0;

        public int Count => _typedData.Count;

        public void Set(IEnumerable<T> elements)
        {
            var targetList = elements.ToList();

            // for undo and event args
            var prevList = _typedData;

            // can simply reassign list, as below, but only if first all the necessary event handlers are removed and added
            //TODO tfs: I'm pretty sure release should always be called after UpdateOnServer, so that we can never reference deleted fields
            foreach (var d in _typedData)
            {
                ReleaseContainedField(d);
            }
            foreach (var d in targetList)
            {
                ReferenceContainedField(d);
            }
            _typedData = targetList;
            // updates the data of the list model @database
            ListModel.Data = targetList.Select(f => f.Id).ToList();

            var newEvent = new UndoCommand(() => Set(targetList), () => Set(prevList));
            UpdateOnServer(newEvent);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Replace, targetList, prevList, 0));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, targetList, prevList));
        }

        public bool ContainsBase(FieldControllerBase element) => element is T checkedElement && Contains(checkedElement);

        public void Set(IEnumerable<FieldControllerBase> fmcs)
        {
            Set(fmcs.OfType<T>());
        }

        #endregion

        #region // OVERLOADED CONSTRUCTORS, INITIALIZATION //

        private bool _initialized = true;

        // Parameterless
        public ListController() : base(new ListModel(new List<string>(), TypeInfoHelper.TypeToTypeInfo(typeof(T))))
        {
            _typedData = new List<T>();
            ConstructorHelper(false);
        }

        // IEnumerable<T> (list of items)
        public ListController(IEnumerable<T> list, bool readOnly = false) : base(new ListModel(list.Select(fmc => fmc.Id), TypeInfoHelper.TypeToTypeInfo(typeof(T))))
        {
            _typedData = new List<T>(list);
            ConstructorHelper(readOnly);
        }

        // T (item)
        public ListController(T item, bool readOnly = false) : base(new ListModel(new List<T> { item }.Select(fmc => fmc.Id), TypeInfoHelper.TypeToTypeInfo(typeof(T))))
        {
            _typedData = new List<T> { item };
            ConstructorHelper(readOnly);
        }

        private ListController(ListModel model) : base(model)
        {
            _initialized = false;
        }

        public static ListController<T> CreateFromServer(ListModel model)
        {
            Debug.Assert(!model.SubTypeInfo.Equals(TypeInfo.None));
            Debug.Assert(TypeInfoHelper.TypeToTypeInfo(typeof(T)) == model.SubTypeInfo);
            return new ListController<T>(model);
        }

        public override async Task InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            var fields = await RESTClient.Instance.Fields.GetControllersAsync<T>(ListModel.Data);
            List<T> list = fields as List<T> ?? new List<T>(fields);

            // furthermore, confirms the type of the list in the model matches the type of this list controller
            _typedData = list;
            foreach (var field in list)
            {
                ReferenceContainedField(field);
            }
        }

        /*
         * Factors out code common to all constructors - sets the readonly status, saves to database and calls the custom initialization
         */
        private void ConstructorHelper(bool readOnly)
        {
            IsReadOnly = readOnly;
        }

        protected override IEnumerable<FieldControllerBase> GetReferencedFields()
        {
            return _typedData;
        }

        private void ReferenceContainedField(T field)
        {
            ReferenceField(field);
            if (IsReferenced)
            {
                field.FieldModelUpdated += ContainedFieldUpdated;
            }
        }

        private void ReleaseContainedField(T field)
        {
            if (IsReferenced)
            {
                field.FieldModelUpdated -= ContainedFieldUpdated;
            }

            ReleaseField(field);
        }

        protected override void RefInit()
        {
            foreach (var fieldControllerBase in _typedData)
            {
                ReferenceContainedField(fieldControllerBase);
            }
        }

        protected override void RefDestroy()
        {
            foreach (var fieldControllerBase in _typedData)
            {
                ReleaseContainedField(fieldControllerBase);
            }
        }

        #endregion

        #region // ACCESSORS //

        // @IList<T> //
        /*
         * Bool used throughout this class to determine whether mutator actions are actually carried out
         */
        public bool IsReadOnly
        {
            // bool value read from and written to the model itself @database
            get => ListModel.IsReadOnly;
            set => ListModel.IsReadOnly = value;
        }

        /*
         * Accesses the controller's underlying ListModel - as of 6/27/18, contains <List<string>> Data, <bool> IsReadOnly and <type> SubTypeInfo, 
         */
        public ListModel ListModel => Model as ListModel;

        // @IList<T> //
        /*
         * Returns the zero-based index of the specified element in the list. If absent, returns -1
         */
        public int IndexOf(T element) => _typedData.IndexOf(element);

        // @IList<T> //
        /*
         * Returns whether or not the specified element is present in the list
         */
        public bool Contains(T element) => _typedData.Contains(element);

        // @IList<T> //
        /*
         * Enables indexing of the list *controller* as one might otherwise carry out on an actual List<T>
         */
        public T this[int index]
        {
            get => _typedData[CheckedIndex(index, _typedData)];
            set
            {
                index = CheckedIndex(index, _typedData);

                var prevElement = _typedData[index]; // for undo and event args
                ReleaseContainedField(prevElement);

                _typedData[index] = value;
                ListModel.Data[index] = value.Id;

                ReferenceContainedField(value);

                var newEvent = new UndoCommand(() => this[index] = value, () => this[index] = prevElement);
                UpdateOnServer(newEvent);

                OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Replace, new List<T> { value }, new List<T> { prevElement }, index));
                //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new List<T> { value }, new List<T> { prevElement }));

            }
        }

        /*
         * Gets the type of the elements in the actual list
         */
        public TypeInfo ListSubTypeInfo { get; } = TypeInfoHelper.TypeToTypeInfo(typeof(T));

        /*
         * Creates and returns a duplicate of this ListController and its underlying data
         */
        public override FieldControllerBase Copy() => new ListController<T>(new List<T>(_typedData));

        /*
         * Creates and returns an empty list of the specified type T
         */
        public override FieldControllerBase GetDefaultController() => new ListController<T>();

        /*
         * Recursive search of list for Dash's search functionality
         */
        public override StringSearchModel SearchForString(Search.SearchMatcher matcher)
        {
            //TODO We should cache the result instead of calling Search for string on the same controller twice, 
            //and also we should probably figure out how many things in TypedData match, and use that for ranking
            return _typedData.FirstOrDefault(controller => controller.SearchForString(matcher).StringFound)?.SearchForString(matcher) ?? StringSearchModel.False;
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return "[" + string.Join(", ", _typedData.Select(f => f.ToScriptString(thisDoc))) + "]";
        }

        // @IList<T> //
        /*
         * Wraps the CopyTo method in the format mandated by IList<Implementation>
         */
        public void CopyTo(T[] destination, int index) => _typedData.CopyTo(destination, index);

        public override string ToString()
        {
            const int cutoff = 5;
            if (Count == 0) return "[<empty>]";

            string suffix = Count > cutoff ? $", ... +{Count - cutoff}" : "";

            return $"[{string.Join(", ", this.Take(Math.Min(cutoff, Count))) + suffix}]";
        }

        public override object GetValue(Context context) => _typedData.ToList();

        public override bool TrySetValue(object value)
        {
            if (value is IEnumerable<T> list)
            {
                Set(list);
                //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, _typedData, prevList));
                return true;
            }
            return false;
        }

        #endregion

        #region // HELPERS //

        private void ContainedFieldUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            if (!ListContainedFieldFlag.Enabled) return;
            if (args is DocumentController.DocumentFieldUpdatedEventArgs dargs)
            {
                Debug.Assert(sender is T);
                var fieldKey = dargs.Reference.FieldKey;
                if (fieldKey.Equals(KeyStore.TitleKey) || fieldKey.Equals(KeyStore.PositionFieldKey)/* || fieldKey.Equals(KeyStore.HiddenKey)*/)
                {
                    OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Content, new List<T> { (T)sender }, null, 0), context);
                }
            }
        }

        private static int CheckedIndex(int raw, ICollection target)
        {
            int len = target.Count;
            if (raw > len) throw new ArgumentOutOfRangeException();

            int safe = raw;
            if (raw < 0) safe = len + (raw % len);
            return safe;
        }

        #endregion

        #region // ADDITION AND INSERTION //

        public void AddBase(FieldControllerBase element)
        {
            if (element is T checkedElement) Add(checkedElement);
        }

        public void InsertBase(int index, FieldControllerBase element)
        {
            if (element is T checkedElement) Insert(index, checkedElement);
        }

        // @IList<T> //
        public void Add(T element)
        {
            if (IsReadOnly) return;
            var prevList = new List<T>(_typedData);
            if (!AddHelper(element)) return;

            var newEvent = new UndoCommand(() => Add(element), () => Remove(element));

            UpdateOnServer(newEvent);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Add, new List<T> { element }, prevList, prevList.Count));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T> { element }));
        }

        private bool AddHelper(T element)
        {
            Debug.Assert(element != null);
            if (AvoidDuplicates) if (_typedData.Contains(element)) return false; // Conditionally avoid duplicate addition

            //TODO tfs: Remove deleted fields from the list if we can delete fields 
            _typedData.Add(element);
            ListModel.Data.Add(element.Id);

            ReferenceContainedField(element);

            return true;
        }

        public void AddRange(IEnumerable<FieldControllerBase> elements)
        {
            AddRange(elements.OfType<T>().ToList());
        }

        public void SetValue(int index, FieldControllerBase field)
        {
            if (field is T tValue)
            {
                this[index] = tValue;
            }
        }

        public FieldControllerBase GetValue(int index)
        {
            return this[index];
        }

        public void AddRange(IEnumerable<T> elements)
        {
            if (IsReadOnly)
            {
                return;
            }
            var prevList = _typedData.ToList();
            var enumerable = elements.ToList();
            foreach (var element in enumerable)
            {
                AddHelper(element);
                //TODO tfs: Remove deleted elements from the list when they are deleted if we can delete fields 
                // Or just use reference counting if that ever gets implemented
            }

            var newEvent = new UndoCommand(() => AddRange(enumerable), () =>
            {
                foreach (var element in enumerable)
                {
                    Remove(element);
                }
            });

            UpdateOnServer(newEvent);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Add, enumerable.ToList(), prevList, prevList.Count));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, enumerable.ToList()));
        }

        // @IList<T> //
        public void Insert(int index, T element)
        {
            if (IsReadOnly)
            {
                return;
            }
            var prevList = _typedData.ToList();
            index = CheckedIndex(index, _typedData);

            _typedData.Insert(index, element);
            ListModel.Data.Insert(index, element.Id);

            ReferenceContainedField(element);

            var newEvent = new UndoCommand(() => Insert(index, element), () => Remove(element));
            UpdateOnServer(newEvent);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Add, new List<T> { element }, prevList, index));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T> { element }));
        }

        #endregion

        #region // REMOVAL //

        public bool Remove(FieldControllerBase element)
        {
            if (element is T checkedElement) return Remove(checkedElement);
            return false;
        }

        // @IList<T> //
        public bool Remove(T element)
        {
            if (IsReadOnly)
            {
                return false;
            }
            var prevIndex = IndexOf(element);

            var success = RemoveHelper(element);
            if (!success) return false;

            var newEvent = new UndoCommand(() => Remove(element), () => Insert(prevIndex, element));

            UpdateOnServer(newEvent);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Remove, _typedData, new List<T> { element }, prevIndex));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<T> { element }));

            return true;
        }

        private bool RemoveHelper(T element)
        {

            var removed = _typedData.Remove(element);
            if (removed)
            {
                ReleaseContainedField(element);
                ListModel.Data.Remove(element.Id);
            }

            return removed;
        }

        // @IList<T> //
        public void RemoveAt(int index)
        {
            if (IsReadOnly)
            {
                return;
            }
            index = CheckedIndex(index, _typedData);
            var element = RemoveAtHelper(index);
            if (element == null) return;

            var newEvent = new UndoCommand(() => RemoveAt(index), () => Insert(index, element));

            UpdateOnServer(newEvent);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Remove, _typedData, new List<T> { element }, index));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<T> { element }));
        }

        private T RemoveAtHelper(int index)
        {
            var element = _typedData[index];
            ReleaseContainedField(element);

            _typedData.RemoveAt(index);
            ListModel.Data.RemoveAt(index);

            return element;
        }

        #endregion

        #region // CLEAR //

        // @IList<T> //
        public void Clear()
        {
            if (IsReadOnly)
            {
                return;
            }
            var prevList = new List<T>(_typedData);
            foreach (var element in _typedData)
            {
                ReleaseContainedField(element);
            }
            _typedData.Clear();
            ListModel.Data.Clear();

            var newEvent = new UndoCommand(Clear, () => Set(prevList));

            UpdateOnServer(newEvent);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Clear, _typedData, prevList, 0));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        #endregion

        #region // ENUMERATORS //

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // @IList<T> //
        public IEnumerator<T> GetEnumerator() => _typedData.GetEnumerator();

        #endregion

        #region // ListFieldUpdatedEventArgs //

        /// <summary>
        /// Provides data about how the list changed. Similar to NotifyCollectionChangedEventArgs.
        /// </summary>
        public class ListFieldUpdatedEventArgs : FieldUpdatedEventArgs
        {
            public enum ListChangedAction
            {
                Add, //Item was added to the list
                Remove, //Items were removed from the list
                Replace, //Items in the list were replaced with other items
                Clear, //The list was cleared
                Content //An item in the list was updated
            }

            public readonly ListChangedAction ListAction;
            public readonly List<T> NewItems;
            public readonly List<T> OldItems;
            public readonly int StartingChangeIndex;

            private ListFieldUpdatedEventArgs() : base(TypeInfo.List, DocumentController.FieldUpdatedAction.Update)
            {
            }

            public ListFieldUpdatedEventArgs(ListChangedAction action) : this()
            {
                if (action != ListChangedAction.Clear)
                    throw new ArgumentException();
                ListAction = action;
                NewItems = null;
                OldItems = null;
                StartingChangeIndex = -1;
            }

            public ListFieldUpdatedEventArgs(ListChangedAction action, List<T> newItems, List<T> oldItems, int changeIndex) : this()
            {
                ListAction = action;
                NewItems = newItems;
                OldItems = oldItems;
                StartingChangeIndex = changeIndex;
            }
        }

        #endregion

        public override bool CheckType(FieldControllerBase fmc)
        {
            bool isList = base.CheckType(fmc);
            if (isList)
            {
                if (!(fmc is IListController list))
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
