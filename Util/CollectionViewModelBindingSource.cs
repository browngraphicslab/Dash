using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Dash
{
    public class CollectionViewModelBindingSource : INotifyCollectionChanged, IItemsRangeInfo, IList
    {
        private List<DocumentViewModel> _cachedViewModels = new List<DocumentViewModel>();
        private DocumentCollectionFieldModelController _collection;
        private int _startIndex = 0, _endIndex = 0;
        private int _bufferSize = 2;

        public CollectionViewModelBindingSource(DocumentCollectionFieldModelController collection)
        {
            _collection = collection;
        }

        #region INotifyCollectionChanged Implementation

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        #endregion

        #region IItemsRangeInfo Implementation

        public void Dispose()
        {
            foreach (var documentViewModel in _cachedViewModels)
            {
                documentViewModel.Dispose();
            }
            _cachedViewModels.Clear();
            _cachedViewModels = null;
        }

        public void RangesChanged(ItemIndexRange visibleRange, IReadOnlyList<ItemIndexRange> trackedItems)
        {
            int startIndex = int.MaxValue;
            int endIndex = int.MinValue;
            foreach (var itemIndexRange in trackedItems)
            {
                startIndex = Math.Min(startIndex, itemIndexRange.FirstIndex);
                endIndex = Math.Max(endIndex, itemIndexRange.LastIndex);
            }

            startIndex = Math.Max(startIndex - _bufferSize, 0);
            endIndex = Math.Max(endIndex + _bufferSize, Count);

            if (startIndex == _startIndex && endIndex == _endIndex)
            {
                return;
            }

            int length = endIndex - startIndex;
            List<DocumentViewModel> newViewModels = new List<DocumentViewModel>(length);
            var docs = _collection.GetDocuments();
            for (int i = 0; i < length; ++i)
            {
                if (i >= _startIndex && i < _endIndex)
                {
                    newViewModels.Add(_cachedViewModels[i - _startIndex]);
                }
                else
                {
                    newViewModels.Add(new DocumentViewModel(docs[startIndex + i]));
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

            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newViewModels, startIndex));
        }

        #endregion

        #region IList Implementation

        public int Count { get; }

        public bool Contains(object value)
        {
            return IndexOf(value) != -1;
        }

        public int IndexOf(object value)
        {
            var vm = value as DocumentViewModel;
            if (vm == null) return -1;
            return _collection.GetDocuments().IndexOf(vm.DocumentController);
        }

        public object this[int index]
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

        #endregion

        #region IList functions not implemented

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public bool IsSynchronized => throw new NotImplementedException();
        public object SyncRoot => throw new NotImplementedException();

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public bool IsFixedSize => false;
        public bool IsReadOnly => false;


        #endregion
    }
}
