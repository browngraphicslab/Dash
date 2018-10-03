using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using DashShared;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public static class ListContainedFieldFlag
    {
        public static bool Enabled = false;
    }

    public class ListController<T> : BaseListController, /*/*INotifyCollectionChanged, */IList<T> where T : FieldControllerBase
    {
        private const bool AvoidDuplicates = false;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #region // DATA //

        // @BaseListController //
        /*
         * Overriden data accessor casts the list type to FieldControllerBase
         */
        public override List<FieldControllerBase> Data
        {
            get => TypedData.Cast<FieldControllerBase>().ToList();
            set
            {
                TypedData = value.Cast<T>().ToList();
                //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace));
            }
        }

        //private void OnCollectionChanged(NotifyCollectionChangedEventArgs args) => CollectionChanged?.Invoke(this, args);

        /*
         * Wrapper to retrieve the list items stored in the ListController.
         */
        private List<T> _typedData = new List<T>();
        public List<T> TypedData
        {
            get => _typedData;
            set => SetTypedData(value);
        }

        public bool IsEmpty => Count == 0;

        /*
         * Sets the data property and gives UpdateOnServer an UndoCommand 
         */
        private void SetTypedData(List<T> targetList, bool withUndo = true)
        {
            if (_typedData == targetList) return; // avoids redundantly assigning itself to an identical list

            // for undo and event args
            var prevList = _typedData;

            // can simply reassign list, as below, but only if first all the necessary event handlers are removed and added
            foreach (var d in _typedData)
            {
                d.FieldModelUpdated -= ContainedFieldUpdated;
            }
            foreach (var d in targetList)
            {
                d.FieldModelUpdated += ContainedFieldUpdated;
            }
            _typedData = targetList;
            // updates the data of the list model @database
            ListModel.Data = targetList.Select(f => f.Id).ToList();

            var newEvent = new UndoCommand(() => SetTypedData(targetList, false), () => SetTypedData(prevList, false));
            UpdateOnServer(withUndo ? newEvent : null);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Replace, targetList, prevList, 0));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, targetList, prevList));
        }

        public void Set(IEnumerable<T> elements, bool withUndo = true)
        {
            if (IsReadOnly) return;

            // for undo and event args
            var prevList = TypedData;
            var newEvent = new UndoCommand(() => Set(elements, false), () => Set(prevList, false));

            // delete everything in TypedData...
            foreach (var element in TypedData)
            {
                RemoveHelper(element);
            }

            // ...and replace it with elements
            var enumerable = elements as List<T> ?? elements.ToList();
            foreach (var element in enumerable)
            {
                AddHelper(element);
            }

            UpdateOnServer(withUndo ? newEvent : null);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Replace, enumerable, prevList, 0));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, enumerable, prevList));
        }

        #endregion

        #region // OVERLOADED CONSTRUCTORS, INITIALIZATION //

        // List model
        public ListController(ListModel model, bool readOnly = false) : base(model) => IsReadOnly = readOnly;

        // Parameterless
        public ListController() : base(new ListModel(new List<string>(), TypeInfoHelper.TypeToTypeInfo(typeof(T)))) => ConstructorHelper(false);

        // IEnumerable<T> (list of items)
        public ListController(IEnumerable<T> list, bool readOnly = false) : base(new ListModel(list.Select(fmc => fmc.Id ), TypeInfoHelper.TypeToTypeInfo(typeof(T)))) => ConstructorHelper(readOnly);

        // T (item)
        public ListController(T item, bool readOnly = false) : base(new ListModel(new List<T> { item }.Select(fmc => fmc.Id ), TypeInfoHelper.TypeToTypeInfo(typeof(T)))) => ConstructorHelper(readOnly);

        /*
         * Factors out code common to all constructors - sets the readonly status, saves to database and calls the custom initialization
         */
        private void ConstructorHelper(bool readOnly)
        {
            IsReadOnly = readOnly;
            Indexed = true;
            SaveOnServer();
            Init();
        }

        public override void Init()
        {
            // ensures that the list isn't initialized with a type of none
            Debug.Assert(!((ListModel)Model).SubTypeInfo.Equals(TypeInfo.None));

            TypedData = ContentController<FieldModel>.GetControllers<T>(ListModel.Data).ToList();

            // furthermore, confirms the type of the list in the model matches the type of this list controller
            Debug.Assert(TypeInfoHelper.TypeToTypeInfo(typeof(T)) == ListModel.SubTypeInfo);
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
        public int IndexOf(T element) => TypedData.IndexOf(element);

        // @IList<T> //
        /*
         * Returns whether or not the specified element is present in the list
         */
        public bool Contains(T element) => TypedData.Contains(element);

        // @IList<T> //
        /*
         * Enables indexing of the list *controller* as one might otherwise carry out on an actual List<T>
         */
        public T this[int index]
        {
            get => TypedData[CheckedIndex(index, TypedData)];
            set => SetIndex(index, value);
        }

        private void SetIndex(int index, T value, bool withUndo = true)
        {
            index = CheckedIndex(index, TypedData);

            var prevElement = TypedData[index]; // for undo and event args

            TypedData[index] = value;
            ListModel.Data[index] = value.Id;

            var newEvent = new UndoCommand(() => SetIndex(index, value, false), () => SetIndex(index, prevElement, false));
            UpdateOnServer(withUndo ? newEvent : null);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Replace, new List<T> { value }, new List<T> { prevElement }, index));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new List<T> { value }, new List<T> { prevElement }));
        }

        //TODO: Remove this accessor - leverage new functionality to improve encapsulation
        public List<T> GetElements() => TypedData.ToList();

        /*
         * Gets the type of the elements in the actual list
         */
        public override TypeInfo ListSubTypeInfo { get; } = TypeInfoHelper.TypeToTypeInfo(typeof(T));

        /*
         * Returns a view of the given list in the form of a table
         */
        public override FrameworkElement GetTableCellView(Context context)
        {
            return GetTableCellViewForCollectionAndLists("📜", delegate (TextBlock block)
            {
                block.Text = string.Format($"{TypedData.Count()} object(s)");           //TODO make a factory and specify what objects it contains ,,,, 
            });
        }

        /*
         * Creates and returns a duplicate of this ListController and its underlying data
         */
        public override FieldControllerBase Copy() => new ListController<T>(new List<T>(TypedData));

        /*
         * Creates and returns an empty list of the specified type T
         */
        public override FieldControllerBase GetDefaultController() => new ListController<T>();

        /*
         * Recursive search of list for Dash's search functionality
         */
        public override StringSearchModel SearchForString(string searchString)
        {
            if (string.IsNullOrEmpty(searchString))
            {
                return new StringSearchModel(true, ToString());
            }
            //TODO We should cache the result instead of calling Search for string on the same controller twice, 
            //and also we should probably figure out how many things in TypedData match, and use that for ranking
            return TypedData.FirstOrDefault(controller => controller.SearchForString(searchString).StringFound)?.SearchForString(searchString) ?? StringSearchModel.False;
        }

        public override string ToScriptString(DocumentController thisDoc)
        {
            return "[" + string.Join(", ", TypedData.Select(f => f.ToScriptString(thisDoc))) + "]";
        }

        // @IList<T> //
        /*
         * Wraps the CopyTo method in the format mandated by IList<Implementation>
         */
        public void CopyTo(T[] destination, int index) => TypedData.CopyTo(destination, index);

        public override string ToString()
        {
            const int cutoff = 5;
            if (Count == 0) return "[<empty>]";

            string suffix = Count > cutoff ? $", ... +{Count - cutoff}" : "";

            return $"[{string.Join(", ", this.Take(Math.Min(cutoff, Count))) + suffix}]";
        }

        public override object GetValue(Context context) => TypedData.ToList();

        public override bool TrySetValue(object value)
        {
            if (value is List<T> list)
            {
                var prevList = TypedData;
                TypedData = list;
                //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, TypedData, prevList));
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

        public override void AddBase(FieldControllerBase element)
        {
            if (element is T checkedElement) Add(checkedElement);
        }

        // @IList<T> //
        public void Add(T element)
        {
            if (!IsReadOnly) AddManager(element);
        }

        private void AddManager(T element, bool withUndo = true)
        {
            if (!AddHelper(element)) return;

            var prevList = TypedData;
            var newEvent = new UndoCommand(() => AddManager(element, false), () => RemoveManager(element, false));

            UpdateOnServer(withUndo ? newEvent : null);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Add, new List<T> { element }, prevList, prevList.Count - 1));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T> { element }));
        }

        private bool AddHelper(T element)
        {
            if (AvoidDuplicates) if (TypedData.Contains(element)) return false; // Conditionally avoid duplicate addition

            element.FieldModelUpdated += ContainedFieldUpdated;

            //TODO tfs: Remove deleted fields from the list if we can delete fields 
            TypedData.Add(element);
            ListModel.Data.Add(element.Id );
            return true;
        }
        
        public static explicit operator ListController<T>(FieldUpdatedEventArgs v)
        {
            throw new NotImplementedException();
        }

        public override void AddRange(IEnumerable<FieldControllerBase> elements)
        {
            if (!IsReadOnly) AddRangeManager(elements.OfType<T>().ToList());
        }

        public override void SetValue(int index, FieldControllerBase field)
        {
            if (field is T tValue)
            {
                this[index] = tValue;
            }
        }

        public override FieldControllerBase GetValue(int index)
        {
            return this[index];
        }

        public void AddRange(IEnumerable<T> elements)
        {
            if (!IsReadOnly) AddRangeManager(elements);
        }

        private void AddRangeManager(IEnumerable<T> elements, bool withUndo = true)
        {
            if (IsReadOnly) return;

            var prevList = TypedData.ToList();
            var enumerable = elements.ToList();
            foreach (var element in enumerable)
            {
                AddHelper(element);
                //TODO tfs: Remove deleted elements from the list when they are deleted if we can delete fields 
                // Or just use reference counting if that ever gets implemented
            }

            var newEvent = new UndoCommand(() => AddRangeManager(enumerable, false), () =>
            {
                foreach (var element in enumerable)
                {
                    RemoveManager(element, false);
                }
            });

            UpdateOnServer(withUndo ? newEvent : null);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Add, enumerable.ToList(), prevList, prevList.Count));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, enumerable.ToList()));
        }

        // @IList<T> //
        public void Insert(int index, T element)
        {
            if (!IsReadOnly) InsertManager(index, element);
        }

        public void InsertManager(int index, T element, bool withUndo = true)
        {
            var prevList = TypedData;
            index = CheckedIndex(index, TypedData);

            TypedData.Insert(index, element);
            ListModel.Data.Insert(index, element.Id);

            var newEvent = new UndoCommand(() => InsertManager(index, element, false), () => RemoveManager(element, false));
            UpdateOnServer(withUndo ? newEvent : null);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Add, new List<T> { element }, prevList, index));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T> { element }));
        }

        #endregion

        #region // REMOVAL //

        public override void Remove(FieldControllerBase element)
        {
            if (element is T checkedElement && !IsReadOnly) Remove(checkedElement);
        }

        // @IList<T> //
        public bool Remove(T element) => !IsReadOnly && RemoveManager(element);

        private bool RemoveManager(T element, bool withUndo = true)
        {
            var prevIndex = IndexOf(element);

            var success = RemoveHelper(element);
            if (!success) return false;

            var newEvent = new UndoCommand(() => RemoveManager(element, false), () => InsertManager(prevIndex, element, false));

            UpdateOnServer(withUndo ? newEvent : null);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Remove, TypedData, new List<T> { element }, prevIndex));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<T> { element }));

            return true;
        }

        private bool RemoveHelper(T element)
        {
            element.FieldModelUpdated -= ContainedFieldUpdated;

            var removed = TypedData.Remove(element);
            ListModel.Data.Remove(element.Id);

            return removed;
        }

        // @IList<T> //
        public void RemoveAt(int index)
        {
            if (!IsReadOnly) RemoveAtManager(index);
        }

        private void RemoveAtManager(int index, bool withUndo = true)
        {
            index = CheckedIndex(index, TypedData);
            var element = RemoveAtHelper(index);
            if (element == null) return;

            var newEvent = new UndoCommand(() => RemoveAtManager(index, false), () => InsertManager(index, element, false));

            UpdateOnServer(withUndo ? newEvent : null);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Remove, TypedData, new List<T> { element }, index));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<T> { element }));
        }

        private T RemoveAtHelper(int index)
        {
            var element = TypedData[index];
            element.FieldModelUpdated -= ContainedFieldUpdated;

            TypedData.Remove(element);
            ListModel.Data.Remove(element.Id);

            return element;
        }

        #endregion

        #region // CLEAR //

        // @IList<T> //
        public void Clear()
        {
            if (!IsReadOnly) ClearManager();
        }

        private void ClearManager(bool withUndo = true)
        {
            var prevList = TypedData;
            foreach (var element in TypedData)
            {
                element.FieldModelUpdated -= ContainedFieldUpdated;
            }
            TypedData.Clear();
            ListModel.Data.Clear();

            var newEvent = new UndoCommand(() => ClearManager(false), () => SetTypedData(prevList, false));

            UpdateOnServer(withUndo ? newEvent : null);

            OnFieldModelUpdated(new ListFieldUpdatedEventArgs(ListFieldUpdatedEventArgs.ListChangedAction.Clear, TypedData, prevList, 0));
            //OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        #endregion

        #region // ENUMERATORS //

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // @IList<T> //
        public IEnumerator<T> GetEnumerator() => TypedData.GetEnumerator();

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
    }
}
