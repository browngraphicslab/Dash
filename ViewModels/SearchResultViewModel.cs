using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Dash.Annotations;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class SearchResultViewModel : INotifyPropertyChanged
    {
        private string _currentTitle;
        private string _currentContext;
        private int _currentIndex;
        private string _docIcon;
        public bool IsCopy = false;
        public int _copies = 1;
        public List<string> Titles { get; }
        public List<string> ContextualTexts { get; }
        public DocumentController ViewDocument { get; }
        public DocumentController DocumentCollection { get; set; }
        public bool IsLikelyUsefulContextText { get; }
        public int FieldCount { get; }

        public int Copies => _copies;

        public string CurrentTitle
        {
            get => _currentTitle;
            private set
            {
                _currentTitle = value;
                Debug.WriteLine("current title: "+_currentTitle);
                OnPropertyChanged();
            }
        }

        public string CurrentContext
        {
            get => _currentContext;
            private set
            {
                _currentContext = "Value: "+ value;
                Debug.WriteLine("current context: " + _currentContext);
                OnPropertyChanged();
            }
        }

        public int CurrentIndex
        {
            get => _currentIndex + 1;
            private set
            {
                _currentIndex = value;
                OnPropertyChanged();
            }
        }

        public string DocIcon
        {
            get => _docIcon;
            private set
            {
                _docIcon = value;
                OnPropertyChanged();
            }
        }

        public SearchResultViewModel(List<string> titles, List<string> contextualTexts, DocumentController viewDoc, DocumentController documentCollectionController, bool isLikelyUsefulContextText = false)
        {
            ContextualTexts = contextualTexts.Select(t => t.Replace("\r", " \\r ").Replace("\n", " \\n ").Replace("\t", " \\t ")).ToList();
            Titles = titles;
            FieldCount = Titles.Count; 
            ViewDocument = viewDoc;
            DocumentCollection = documentCollectionController;
            IsLikelyUsefulContextText = isLikelyUsefulContextText;

            CurrentIndex = 0;

            if (Titles.Any())
            {
                UpdateText();
            }

            var type = viewDoc.GetDocType();
            Debug.WriteLine(type);
            switch (type)
            {
            case "Image Box":
                DocIcon = (string)Application.Current.Resources["ImageIcon"];
                break;
            case "Rich Text Box":
                DocIcon = (string)Application.Current.Resources["DocumentIcon"];
                break;
            case "Collection Box":
                DocIcon = (string)Application.Current.Resources["CollectionIcon"];
                break;
            case "Pdf Box":
                DocIcon = (string)Application.Current.Resources["PdfDocumentIcon"];
                break;
            case "Audio Box":
                DocIcon = (string)Application.Current.Resources["AudioIcon"];
                break;
            case "Video Box":
                DocIcon = (string)Application.Current.Resources["VideoIcon"];
                break;
            default:
                DocIcon = (string)Application.Current.Resources["DocumentIcon"];
                break;
            }
        }

        private void UpdateText()
        {
            CurrentTitle = Titles[_currentIndex];
            CurrentContext = ContextualTexts[_currentIndex];
        }

        public void NextField()
        {
            CurrentIndex = CurrentIndex;
            if (_currentIndex == FieldCount) CurrentIndex = 0;
            UpdateText();
        }

        public void PreviousField()
        {
            CurrentIndex = CurrentIndex - 2;
            if (_currentIndex == -1) CurrentIndex = FieldCount - 1;
            UpdateText();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
