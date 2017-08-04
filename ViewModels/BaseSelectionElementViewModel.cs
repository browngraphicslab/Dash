using System;
using System.Diagnostics;
using Windows.UI.Xaml;

namespace Dash
{
    public abstract class BaseSelectionElementViewModel : ViewModelBase
    {
        private bool _isSelected;
        private bool _isLowestSelected;

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

        public void SetSelected(FrameworkElement setter, bool isSelected)
        {
            Debug.Assert(ReferenceEquals(setter.DataContext, this), "selection should only be set by the views which have this as a datacontext");
            IsSelected = isSelected;
        }

        public void SetLowestSelected(FrameworkElement setter, bool isLowestSelected)
        {
            Debug.Assert(ReferenceEquals(setter.DataContext, this), "selection should only be set by the views which have this as a datacontext");
            IsLowestSelected = isLowestSelected;
        }
    }
}
