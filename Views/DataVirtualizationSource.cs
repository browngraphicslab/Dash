using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Org.BouncyCastle.Security;

namespace Dash
{
    public class DataVirtualizationSource<T> : IList<ImageSource>, INotifyCollectionChanged, IItemsRangeInfo
    {
        public ItemIndexRange VisibleItemsRange;
        public IReadOnlyList<ItemIndexRange> TrackedItems;
        private ObservableCollection<ImageSource> _cachedItems;
        private int _bufferSize = 2;
        private int _startIndex;
        private int _endIndex;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public DataVirtualizationSource(ScrollViewer scrollViewer) : base()
        {
            _cachedItems = new ObservableCollection<ImageSource>();
        }
        
        public void Dispose()
        {
            _cachedItems.Clear();
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
            endIndex = Math.Min(endIndex + _bufferSize, Count);

            if (startIndex == _startIndex && endIndex == _endIndex)
            {
                return;
            }

            int length = endIndex - startIndex;
            ObservableCollection<ImageSource> newPages = new ObservableCollection<ImageSource>();
            var createdPages = new List<KeyValuePair<int, ImageSource>>();
            for (int i = 0; i < length; ++i)
            {
                if (i + startIndex >= _startIndex && i + startIndex < _endIndex)
                {
                    newPages.Add(_cachedItems[i + startIndex - _startIndex]);
                }
                else
                {
                    var page = _cachedItems[startIndex + 1];
                    newPages.Add(page);
                    createdPages.Add(new KeyValuePair<int, ImageSource>(i + startIndex, page));
                }
            }

            _startIndex = startIndex;
            _endIndex = endIndex;
            _cachedItems = newPages;

            VisibleItemsRange = visibleRange;
            TrackedItems = trackedItems;
            foreach (var createdPage in createdPages)
            {
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                        new List<ImageSource>{createdPage.Value}, null, createdPage.Key));
            }
        }

        public int IndexOf(ImageSource item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, ImageSource item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public ImageSource this[int index]
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
                return _cachedItems[index - _startIndex];
            }
            set
            {
                var imgSrc = value as ImageSource;
                if (imgSrc != null)
                {
                    _cachedItems[index - _startIndex] = imgSrc;
                    CollectionChanged?.Invoke(this,
                        new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, imgSrc, index));
                }
                else
                {
                    throw new InvalidParameterException();
                }
            }
        }

        public void Add(ImageSource item)
        {
            if (item != null)
            {
                _cachedItems.Add(item);
                _endIndex++;
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            }
            else
            {
                throw new InvalidParameterException();
            }
        }

        public void Clear()
        {
            _cachedItems.Clear();
        }

        public bool Contains(ImageSource item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(ImageSource[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(ImageSource item)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get => _cachedItems.Count;
        }
        public bool IsReadOnly { get; }

        public IEnumerator<ImageSource> GetEnumerator()
        {
            return _cachedItems.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
