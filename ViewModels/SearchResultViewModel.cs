using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Dash.Annotations;

// ReSharper disable once CheckNamespace
namespace Dash
{
    public class SearchResultViewModel : INotifyPropertyChanged
    {
        private string _currentTitle;
        private string _currentContext;
        private int _currentIndex;
        public List<string> Titles { get; }
        public List<string> ContextualTexts { get; }
        public DocumentController ViewDocument { get; }
        public DocumentController DocumentCollection { get; set; }
        public bool IsLikelyUsefulContextText { get; }
        public int FieldCount { get; }

        public string CurrentTitle
        {
            get => _currentTitle;
            private set
            {
                _currentTitle = value;
                OnPropertyChanged();
            }
        }

        public string CurrentContext
        {
            get => _currentContext;
            private set
            {
                _currentContext = value;
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

        public SearchResultViewModel(List<string> titles, List<string> contextualTexts, DocumentController viewDoc, DocumentController documentCollectionController, bool isLikelyUsefulContextText = false)
        {
            ContextualTexts = contextualTexts.Select(t => t.Replace("\r", " \\r ").Replace("\n", " \\n ").Replace("\t", " \\t ")).ToList();
            Titles = titles;
            FieldCount = Titles.Count;
            ViewDocument = viewDoc;
            DocumentCollection = documentCollectionController;
            IsLikelyUsefulContextText = isLikelyUsefulContextText;

            CurrentIndex = 0;
            UpdateText();
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
