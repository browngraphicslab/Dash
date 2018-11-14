using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using Windows.UI.Xaml.Data;

namespace Dash
{
    public class CollectionViewModelBindingSource : INotifyCollectionChanged, IItemsRangeInfo, IList<DocumentViewModel>, IList
    {
        private List<DocumentViewModel> _cachedViewModels = new List<DocumentViewModel>();
        private ListController<DocumentController> _collection = null;
        private int _startIndex = 0, _endIndex = 0;
        private int _bufferSize = 2;

        public CollectionViewModelBindingSource()
        {

        }

        public CollectionViewModelBindingSource(ListController<DocumentController> collection)
        {
            _collection = collection;
            collection.FieldModelUpdated += CollectionOnFieldModelUpdated;
        }

        private void CollectionOnFieldModelUpdated(FieldControllerBase sender, FieldUpdatedEventArgs args, Context context)
        {
            var colArgs = (ListController<DocumentController>.ListFieldUpdatedEventArgs)args;
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            //var dvms = new List<DocumentViewModel>();
            //foreach (var colArgsChangedDocument in colArgs.ChangedDocuments)
            //{
            //    dvms.Add(null);
            //}
            //switch (colArgs.CollectionAction)
            //{
            //    case ListController<DocumentController>.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Add:
            //        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, dvms));
            //        break;
            //    case ListController<DocumentController>.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Clear:
            //        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            //        break;
            //    case ListController<DocumentController>.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Remove:
            //        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, dvms));
            //        break;
            //    case ListController<DocumentController>.CollectionFieldUpdatedEventArgs.CollectionChangedAction.Replace:
            //        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, dvms));
            //        break;
            //}
        }

        #region INotifyCollectionChanged Implementation

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region IItemsRangeInfo Implementation

        public void Dispose()
        {
            if (_collection != null)
            {
                _collection.FieldModelUpdated -= CollectionOnFieldModelUpdated;
            }
            foreach (var documentViewModel in _cachedViewModels)
            {
                documentViewModel.Dispose();
            }
            _cachedViewModels.Clear();
            _cachedViewModels = null;
        }

        public void RangesChanged(ItemIndexRange visibleRange, IReadOnlyList<ItemIndexRange> trackedItems)
        {
            if (_collection == null)
            {
                return;
            }
            int startIndex = int.MaxValue;
            int endIndex = int.MinValue;
            foreach (var itemIndexRange in trackedItems)
            {
                startIndex = Math.Min(startIndex, itemIndexRange.FirstIndex);
                endIndex = Math.Max(endIndex, itemIndexRange.LastIndex);
            }

            startIndex = Math.Max(startIndex - _bufferSize, 0);
            endIndex = Math.Min(endIndex + _bufferSize, Count);

            if (startIndex == _startIndex && endIndex == _endIndex)
            {
                return;
            }

            int length = endIndex - startIndex;
            List<DocumentViewModel> newViewModels = new List<DocumentViewModel>(length);
            var createdViewModels = new List<KeyValuePair<int, DocumentViewModel>>();
            for (int i = 0; i < length; ++i)
            {
                if (i + startIndex >= _startIndex && i + startIndex < _endIndex)
                {
                    newViewModels.Add(_cachedViewModels[i + startIndex - _startIndex]);
                }
                else
                {
                    var documentViewModel = new DocumentViewModel(_collection[startIndex + i]);
                    newViewModels.Add(documentViewModel);
                    createdViewModels.Add(new KeyValuePair<int, DocumentViewModel>(i + startIndex, documentViewModel));
                }
            }

            for (int i = _startIndex; i < _endIndex; ++i)
            {
                if (i < startIndex || i >= endIndex)
                {
                    _cachedViewModels[i - _startIndex].Dispose();
                }
            }

            _startIndex = startIndex;
            _endIndex = endIndex;
            _cachedViewModels = newViewModels;

            foreach (var createdViewModel in createdViewModels)
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, createdViewModel.Value, null, createdViewModel.Key));
            }
        }

        #endregion

        #region IList Implementation

        public int Count => _collection.Count;

        public bool Contains(object value)
        {
            return Contains((DocumentViewModel)value);
        }

        public bool Contains(DocumentViewModel item)
        {
            return IndexOf(item) != -1;
        }

        public int IndexOf(object value)
        {
            return IndexOf((DocumentViewModel)value);
        }
        public int IndexOf(DocumentViewModel item)
        {
            if (item == null)
            {
                return -1;
            }
            return _collection.IndexOf(item.DocumentController);
        }

        public DocumentViewModel this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }
                if (index < _startIndex || index >= _endIndex)
                {
                    return null;
                }
                return _cachedViewModels[index - _startIndex];
            }
            set => throw new NotImplementedException();
        }

        object IList.this[int index]
        {
            get => this[index];
            set => throw new NotImplementedException();
        }

        #endregion

        #region IList functions not implemented

        public IEnumerator<DocumentViewModel> GetEnumerator()
        {
            Debug.Assert(_collection == null);
            return _cachedViewModels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            Debug.Assert(_collection == null);
            return _cachedViewModels.GetEnumerator();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(DocumentViewModel[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool IsSynchronized => throw new NotImplementedException();
        public object SyncRoot => throw new NotImplementedException();

        public int Add(object value)
        {
            Add((DocumentViewModel)value);
            return Count - 1;
        }

        public void Add(DocumentViewModel item)
        {
            Debug.Assert(_collection == null);
            _cachedViewModels.Add(item);
        }

        public void Clear()
        {
            Debug.Assert(_collection == null);
            _cachedViewModels.Clear();
        }

        public void Insert(int index, object value)
        {
            Insert(index, (DocumentViewModel)value);
        }

        public void Insert(int index, DocumentViewModel item)
        {
            Debug.Assert(_collection == null);
            _cachedViewModels.Insert(index, item);
        }

        public void Remove(object value)
        {
            Remove((DocumentViewModel)value);
        }

        public bool Remove(DocumentViewModel item)
        {
            Debug.Assert(_collection == null);
            return _cachedViewModels.Remove(item);
        }

        public void RemoveAt(int index)
        {
            Debug.Assert(_collection == null);
            _cachedViewModels.RemoveAt(index);
        }

        public bool IsFixedSize => false;
        public bool IsReadOnly => false;


        #endregion


    }
}
