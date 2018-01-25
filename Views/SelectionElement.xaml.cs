using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Dash
{
    public abstract partial class SelectionElement : UserControl
    {
        private bool _isSelected;
        private bool _isLowestSelected;
        private bool _isLoaded;
        private bool _multiSelectEnabled;
        
        public SelectionElement ParentSelectionElement => this.GetFirstAncestorOfType<SelectionElement>();

        /// <summary>
        /// Enables or disables multi-select mode. Handles overhead.
        /// </summary>
        /// <param name="val"></param>
        public void MultiSelectEnabled(bool val)
        {
            // going from multiSelect to normal select
            if (_multiSelectEnabled && !val)
            {
                SelectedElements.Clear();
                this.Deactivate();
            }
            _multiSelectEnabled = val;
        }

        // single select wraps around the multi select list
        public SelectionElement CurrentSelectedElement {
            get { return SelectedElements?.FirstOrDefault(); }
            private set { SelectedElements.Clear(); SelectedElements.Insert(0, value); }
        }
        public List<SelectionElement> SelectedElements { get; set; }
        public bool HasDragLeft;
        public bool IsSelected
        {
            get => _isSelected;
            private set
            {
                if (_isSelected == value) return;

                _isSelected = value;
                OnActivated(value);
            }
        }

        public bool IsLowestSelected
        {
            get => _isLowestSelected;
            private set
            {
                if (_isLowestSelected == value) return;
                _isLowestSelected = value;
                OnLowestActivated(value);
            }
        }

        public SelectionElement() : base()
        {
            InitializeComponent();
            Loaded += SelectionElement_Loaded;
            Unloaded += SelectionElement_Unloaded;
            SelectedElements = new List<SelectionElement>();
        }

        private void SelectionElement_Unloaded(object sender, RoutedEventArgs e)
        {
            Loaded -= SelectionElement_Loaded;
            Unloaded -= SelectionElement_Unloaded;
        }
        private void SelectionElement_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
        }

        /// <summary>
        /// An abstract method to determine the UI response to user selection of an element.
        /// </summary>
        /// <param name="isSelected"></param>
        protected abstract void OnActivated(bool isSelected);

        /// <summary>
        /// An abstract method to determine the response when the selected element is/is not the lowest selected element.
        /// </summary>
        /// <param name="isLowestSelected"></param>
        protected abstract void OnLowestActivated(bool isLowestSelected);

        /// <summary>
        /// Call this method to set the selection to this selection element
        /// </summary>
        public void OnSelected()
        {
            // if we don't already get the clicks tell our parent we want them
            if (!IsLowestSelected)
            {
                // select us and deselect everything else 
                if (!_multiSelectEnabled)
                {
                    // first deselect all of our children
                    CurrentSelectedElement?.Deactivate();
                }
                // then set up our ancestors
                ParentSelectionElement?.SetAsAncestorOfSelected(this);

                // finally set up our child
                ParentSelectionElement?.SetCurrentlySelectedElement(this);
                
            }
        }

        private void SetAsAncestorOfSelected(SelectionElement selected)
        {
            // if we are an ancestor of a selected element we are selected but not the lowest selected
            IsSelected = true;
            IsLowestSelected = false;

            // if we had a child on a different branch deactivate that branch of children
            if (!_multiSelectEnabled && CurrentSelectedElement != null && CurrentSelectedElement != selected)
            {
                CurrentSelectedElement.Deactivate();
            }

            // set the child to be the newly selected element
            if (!_multiSelectEnabled)
                CurrentSelectedElement = selected;
            else
                SelectedElements.Add(selected);

            // recursively set our parents to have the correct ancestors
            ParentSelectionElement?.SetAsAncestorOfSelected(this);
        }

        /// <summary>
        /// Called only if the SelectionElement is the MainPage document.
        /// </summary>
        public void InitializeAsRoot()
        {
            if (_isLoaded)
            {
                OnSelected();
            }
            else
            {
                Loaded += (sender, args) => OnSelected();
            }
        }

        /// <summary>
        /// Selects a child SelectionElement with this as the parent SelectionElement
        /// </summary>
        /// <param name="newSelectedElement"></param>
        private void SetCurrentlySelectedElement(SelectionElement newSelectedElement)
        {
            // if only in single selection mode
            if (!_multiSelectEnabled)
            {
                // if the new selected is different from the current selected element
                if (CurrentSelectedElement != null && CurrentSelectedElement != newSelectedElement)
                {
                    CurrentSelectedElement.Deactivate(); // deactivate the current
                }

                // current = new
                CurrentSelectedElement = newSelectedElement;
            }
            else {
                // add to selected list if non-null in multi select
                if (newSelectedElement != null && !SelectedElements.Contains(newSelectedElement))
                    SelectedElements.Add(newSelectedElement);
            }

            // if new is not null
            if (newSelectedElement != null)
            {
                // new is lowest selected and new is selected
                newSelectedElement.IsSelected = true;
                newSelectedElement.IsLowestSelected = true;

                // the current item is no longer the lowest selected
                IsLowestSelected = false;
            }

            // the current item is selected though
            IsSelected = true;
        }

        private void Deactivate()
        {
            CurrentSelectedElement?.Deactivate();
            IsSelected = false;
            IsLowestSelected = false;
        }
    }
}

