﻿using System;
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
using Dash;
using Dash.Controllers.Operators;
using System.ComponentModel;
using Windows.UI.Input;
using static Windows.ApplicationModel.Core.CoreApplication;
using System.Diagnostics;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionDBSchemaHeader : UserControl
    {
        public class HeaderViewModel : ViewModelBase
        {
            double _width;
            public KeyController          FieldKey;
            public DocumentController     SchemaDocument;
            public CollectionDBSchemaView SchemaView;

            public override string ToString() { return FieldKey.Name; }
            public double Width
            {
                get => _width;
                set => SetProperty(ref _width, value);
            }
        }
        double _startHeaderDragWidth = 0;

        public CollectionDBSchemaHeader()
        {
            this.InitializeComponent();
        }

        public HeaderViewModel ViewModel { get => DataContext as HeaderViewModel;  }
        public CollectionView ParentCollection { get => VisualTreeHelperExtensions.GetFirstAncestorOfType<CollectionView>(this); } 

        /// <summary>
        /// Tapping the headers toggles the sorting of the column (up,down,none)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HeaderTapped(object sender, TappedRoutedEventArgs e)
        {
            var viewModel = (DataContext as HeaderViewModel);
            var collection = VisualTreeHelperExtensions.GetFirstAncestorOfType<CollectionView>(this);
            if (collection != null)
            {
                viewModel.SchemaView.Sort(viewModel);
            }
        }

        /// <summary>
        /// Double tapping the header switches to a DB view for the header's data field
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HeaderDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (ParentCollection != null)
            {
                ViewModel.SchemaDocument.SetField(CollectionDBView.FilterFieldKey, ViewModel.FieldKey, true);
                ParentCollection.SetView(CollectionView.CollectionViewType.DB);
            }
        }

        private void ResizeHandleManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            ViewModel.Width = Math.Max(0, _startHeaderDragWidth + e.Cumulative.Translation.X);
            e.Handled = true;
        }
        
        private void ResizeHandleManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void ResizeHandleManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            e.Handled = true;
        }
        private void ResizeHandle_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.SchemaView.xHeaderView.CanReorderItems = false;
            ViewModel.SchemaView.xHeaderView.CanDragItems = false;
            _startHeaderDragWidth = ViewModel.Width;
        }

        private void ResizeHandle_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.SchemaView.xHeaderView.CanReorderItems = true;
            ViewModel.SchemaView.xHeaderView.CanDragItems = true;
        }

        private void Grid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.GetFirstAncestorOfType<DocumentView>().ManipulationMode = ManipulationModes.None;
        }
    }
}
