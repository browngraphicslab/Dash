using System;
using System.Diagnostics;
using Windows.UI.Xaml;

namespace Dash
{
    public abstract class BaseSelectionElementViewModel : ViewModelBase
    {
        private bool _isSelected;
        private bool _isLowestSelected;
        private bool _previousIsSelected;

        public event Action<bool> OnSelectionSet;
        public event Action<bool> OnLowestSelectionSet;

        public bool IsSelected
        {
            get { return _isSelected; }
            protected set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    OnSelectionSet?.Invoke(value);
                }
            }
        }

        public bool IsLowestSelected
        {
            get { return _isLowestSelected; }
            protected set
            {
                if (SetProperty(ref _isLowestSelected, value))
                {
                    OnLowestSelectionSet?.Invoke(value);
                }
            }
        }

        protected BaseSelectionElementViewModel()
        {
        }

        //  This has to be public but should only be called by views who have this instance as their DataContext
        public void SetSelected(FrameworkElement setter, bool isSelected)
        {
            IsSelected = isSelected;
        }

        //  This has to be public but should only be called by views who have this instance as their DataContext
        public void SetLowestSelected(FrameworkElement setter, bool isLowestSelected)
        {
            IsLowestSelected = isLowestSelected;
        }
        
    }
}
