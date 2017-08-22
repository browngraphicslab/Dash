using System;
using System.Diagnostics;
using Windows.UI.Xaml;

namespace Dash
{
    public abstract class BaseSelectionElementViewModel : ViewModelBase
    {
        private static event Action<bool> GlobalHitTestVisibilityChanged;

        private bool _isSelected;
        private bool _isLowestSelected;
        private bool _previousIsSelected;
        private static bool _globalHitTestVisibility;

        public event Action<bool> OnSelectionSet;
        public event Action<bool> OnLowestSelectionSet;

        /// <summary>
        /// Controls the global hit test visibility on all SelectionElements
        /// </summary>
        public static bool GlobalHitTestVisibility
        {
            get { return _globalHitTestVisibility; }
            private set {
                if (_globalHitTestVisibility == value) return;
                _globalHitTestVisibility = value;
                GlobalHitTestVisibilityChanged?.Invoke(_globalHitTestVisibility);
            }
        }

        public bool IsSelected
        {
            get { return _isSelected || IsInInterfaceBuilder; }
            protected set
            {
                if (SetProperty(ref _isSelected, value || IsInInterfaceBuilder))
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

        protected readonly bool IsInInterfaceBuilder;

        protected BaseSelectionElementViewModel(bool isInInterfaceBuilder)
        {
            IsInInterfaceBuilder = isInInterfaceBuilder;

            GlobalHitTestVisibilityChanged += newVisibility =>
            {
                if (newVisibility)
                {
                    _previousIsSelected = IsSelected;
                    IsSelected = true;
                }
                else
                {
                    IsSelected = _previousIsSelected;
                }
            };
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

        public void SetGlobalHitTestVisiblityOnSelectedItems(bool isHitTestVisible)
        {
            GlobalHitTestVisibility = isHitTestVisible;
        }
    }
}
