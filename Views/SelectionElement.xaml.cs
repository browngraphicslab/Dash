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
        private bool _isLoaded;

        public SelectionElement ParentSelectionElement => this.GetFirstAncestorOfType<SelectionElement>();
        public SelectionElement CurrentSelectedElement { get; private set; }
        public bool HasDragLeft;

        public SelectionElement() : base()
        {
            InitializeComponent();
            Loaded += SelectionElement_Loaded;
            Unloaded += SelectionElement_Unloaded;
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
        /// Call this method to set the selection to this selection element
        /// </summary>
        public void OnSelected()
        {
            // if we don't already get the clicks tell our parent we want them
            // first deselect all of our children
            CurrentSelectedElement?.Deactivate();

            // then set up our ancestors
            ParentSelectionElement?.SetAsAncestorOfSelected(this);

            // finally set up our child
            ParentSelectionElement?.SetCurrentlySelectedElement(this);
        }

        private void SetAsAncestorOfSelected(SelectionElement selected)
        {
            // if we had a child on a different branch deactivate that branch of children
            if (CurrentSelectedElement != null && CurrentSelectedElement != selected)
            {
                CurrentSelectedElement.Deactivate();
            }

            // set the child to be the newly selected element
            CurrentSelectedElement = selected;

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

        private void SetCurrentlySelectedElement(SelectionElement newSelectedElement)
        {
            // if the new selected is different from the current selected element
            if (CurrentSelectedElement != null && CurrentSelectedElement != newSelectedElement)
            {
                CurrentSelectedElement.Deactivate(); // deactivate the current
            }

            // current = new
            CurrentSelectedElement = newSelectedElement;

            // if new is not null
            if (newSelectedElement != null)
            { 

                // deselect all of the newly selected elements children (not sure if necessary)
                newSelectedElement.SetCurrentlySelectedElement(null);
            }
        }

        private void Deactivate()
        {
            CurrentSelectedElement?.Deactivate();
        }
    }
}

