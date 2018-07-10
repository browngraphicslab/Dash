using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;

namespace Dash
{
    public partial class FieldPreview : INotifyPropertyChanged
    {
        private object _previewContent;

        public DocumentController Doc
        {
            get;
            set;
        }

        public object PreviewContent
        {
            get => _previewContent;
            set
            {
                _previewContent = value;
                OnPropertyChanged();
            }
        }

        public FieldPreview()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void FieldPreview_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is DictionaryEntry))
            {
                return;
            }
            var ent = (DictionaryEntry)DataContext;
            var value = new DocumentFieldReference(Doc, ent.Key as KeyController).DereferenceToRoot(null);
            if (value == null)
            {
                return;
            }
            PreviewContent = value.GetValue(null);
        }
    }
}