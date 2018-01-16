using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;

namespace Dash
{
    /// <summary>
    /// Handles word count and font size bindings in RichTextView
    /// </summary>
    public class WordCount : INotifyPropertyChanged
    {
        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                RaisePropertyChanged("Count");
            }
        }
        private int _count;

        public float Size
        {
            get => _size;
            set
            {
                _size = value;
                SetFontSize();
                RaisePropertyChanged("Size");
            }
        }
        private float _size;
        public event PropertyChangedEventHandler PropertyChanged;

        private RichEditBox _box;
        public WordCount(RichEditBox box)
        {
            _box = box;
        }

        public void CountWords()
        {
            string text;
            _box.Document.GetText(TextGetOptions.None, out text);
            var words = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Count = words.Length;
        }

        private void SetFontSize()
        {
            var selection = _box.Document.Selection;
            if (!selection.CharacterFormat.Size.Equals(_size) && _size > 0)
            {
                selection.CharacterFormat.Size = _size;
            }
        }

        protected void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
