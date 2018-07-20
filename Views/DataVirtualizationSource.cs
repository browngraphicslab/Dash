using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Annotations;
using Org.BouncyCastle.Security;

namespace Dash
{
    public class DataVirtualizationSource<T> : IList, INotifyPropertyChanged, INotifyCollectionChanged, IItemsRangeInfo
    {
        public ItemIndexRange VisibleItemsRange;
        public IReadOnlyList<ItemIndexRange> TrackedItems;
        private ObservableCollection<ImageSource> _cachedItems;
        private ObservableCollection<ImageSource> _images;
        private int _bufferSize = 2;
        private int _startIndex;
        private int _endIndex;

        public DataVirtualizationSource()
        {
            _images = new ObservableCollection<ImageSource>();
            _cachedItems = new ObservableCollection<ImageSource>();
        }
        
        public void Dispose()
        {
            _cachedItems.Clear();
        }

        public void RangesChanged(ItemIndexRange visibleRange, IReadOnlyList<ItemIndexRange> trackedItems)
        {
            var startIndex = visibleRange.FirstIndex;
            var endIndex = visibleRange.FirstIndex + visibleRange.Length;
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

            var length = endIndex - startIndex;
            var newImages = new List<ImageSource>();
            var createdImages = new List<KeyValuePair<int, ImageSource>>();
            var images = _images;
            for (var i = 0; i < length; ++i)
            {
                if (i + startIndex >= _startIndex && i + startIndex < _endIndex)
                {
                    newImages.Add(_cachedItems[i + startIndex - _startIndex]);
                }
                else
                {
                    newImages.Add(_cachedItems[startIndex + i]);
                    createdImages.Add(new KeyValuePair<int, ImageSource>(i + startIndex, _cachedItems  [startIndex + i]));
                }
            }

            _images = new ObservableCollection<ImageSource>(newImages);

            _startIndex = startIndex;
            _endIndex = (int) endIndex;

            VisibleItemsRange = visibleRange;
            TrackedItems = trackedItems;

            foreach (var createdImage in createdImages)
            {
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, createdImage.Value,
                        null, createdImage.Key));
            }
        }

        public void Insert(int index, ImageSource item)
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

        public bool IsFixedSize { get; }

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
                return _cachedItems[index];
            }
            set
            {
                var imgSrc = value as ImageSource;
                if (imgSrc != null)
                {
                    _cachedItems[index] = imgSrc;
                    //CollectionChanged?.Invoke(this,
                    //    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, imgSrc, i ndex));
                }
                else
                {
                    throw new InvalidParameterException();
                }
            }
        }

        object IList.this[int index]
        {
            get => this[index];
            set => throw new NotImplementedException();
        }

        public void Add(ImageSource item)
        {
            if (item != null)
            {
                _images.Add(item);
                _cachedItems.Add(item);
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged("Item[]");
                //CollectionChanged?.Invoke(this,
                //    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _images.IndexOf(item)));
            }
            else
            {
                throw new InvalidParameterException();
            }
        }

        public int Add(object value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            _cachedItems.Clear();
        }

        public bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public bool Contains(ImageSource item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(ImageSource item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get => _cachedItems.Count;
        }

        public bool IsSynchronized { get; }
        public object SyncRoot { get; }
        public bool IsReadOnly { get; }

        public IEnumerator GetEnumerator()
        {
            return _cachedItems.GetEnumerator();
        }

        public new virtual IList ToList()
        {
            return _cachedItems;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ObservableCollection<ImageSource> Get()
        {
            return _images;
        }
    }
}
