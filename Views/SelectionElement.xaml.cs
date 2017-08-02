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
using Windows.UI.Xaml.Navigation;

namespace Dash
{
    public abstract partial class SelectionElement : UserControl
    {
        public SelectionElement ParentSelectionElement => this.GetFirstAncestorOfType<SelectionElement>();

        public SelectionElement SelectedElement;
        private bool _isSelected;
        private bool _isLowestSelected;

        public void SetSelectedElement(SelectionElement elem)
        {
            if (SelectedElement != null && SelectedElement != elem)
            {
                SelectedElement.IsSelected = false;
                if (SelectedElement.SelectedElement != null)
                {
                    SelectedElement.SetSelectedElement(null);
                }
            }
            SelectedElement = elem;
            if (elem != null)
            {
                elem.IsLowestSelected = true;
                elem.SetSelectedElement(null);
                if (IsLowestSelected) IsLowestSelected = false;
            }
            //else if (!IsLowestSelected)
            //{
            //    IsLowestSelected = true;
            //}
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                _isSelected = value;
                if (!value) IsLowestSelected = false;
                OnActivated(value);
            }
        }

        public bool IsLowestSelected
        {
            get { return _isLowestSelected; }
            set
            {
                _isLowestSelected = value;
                if (value) IsSelected = true;
                OnLowestActivated(value);
            }
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
        public abstract void OnLowestActivated(bool isLowestSelected);

        public SelectionElement() : base()
        {
            InitializeComponent();
            Loaded += SelectionElement_Loaded;
        }

        private void SelectionElement_Loaded(object sender, RoutedEventArgs e)
        {
            if (ParentSelectionElement == null) IsSelected = true;
            else IsSelected = false;
        }

        protected void OnSelected()
        {
            if (!IsLowestSelected)
            {
                ParentSelectionElement?.SetSelectedElement(this);
                //SetSelectedElement(null);
            }
            else
            {
                // ParentSelectionElement?.SetSelectedElement(null);
            }
        }
    }
}

