using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class InterfaceBuilder : WindowTemplate
    {

        private DocumentViewModel _documentViewModel;

        private DocumentModel _documentModel;

        private DocumentView _documentView;

        public InterfaceBuilder(DocumentViewModel viewModel,int width=500, int height=500)
        {
            this.InitializeComponent();

            // set width and height to avoid Width = NaN ...
            Width = 500;
            Height = 500;


            _documentViewModel = viewModel;
            _documentModel = viewModel.DocumentModel;
            _documentView = new DocumentView();

            _documentView.DataContext = _documentViewModel;

            Canvas.SetLeft(_documentView, xDocumentsPane.CanvasWidth / 2 - _documentView.Width);
            Canvas.SetTop(_documentView, xDocumentsPane.CanvasHeight / 2 - _documentView.Height);

            xDocumentsPane.Canvas.Children.Add(_documentView);
        }

        private void ApplyEditable()
        {
            List<UIElement> editableElements = new List<UIElement>();

            var elements = _documentView.GetUIElements();
            foreach (var uiElement in elements)
            {
                var left = Canvas.GetLeft(uiElement);
                var top = Canvas.GetTop(uiElement);

                var editableBorder = new EditableFieldFrame {EditableContent = uiElement, BorderBrush = new SolidColorBrush(Colors.HotPink), BorderThickness = new Thickness(5)};
                editableElements.Add(editableBorder);
                Canvas.SetLeft(editableBorder, left);
                Canvas.SetTop(editableBorder, top);

            }


            _documentView.SetUIElements(editableElements);
            //TODO dangerous to add the document repeatedly
        }


        /// <summary>
        /// Needed to make sure that the bounds on the windows size (min and max) don't exceed the size of the free form canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XDocumentsPane_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            FreeformView freeform = sender as FreeformView;
            Debug.Assert(freeform != null);
            this.MaxHeight = HeaderHeight + freeform.CanvasHeight - 5;
            this.MaxWidth = xSettingsPane.ActualWidth + freeform.CanvasWidth;
            this.MinWidth = xSettingsPane.ActualWidth + 50;
            this.MinHeight = HeaderHeight * 2;
        }

        private void ApplyEditableOnTapped(object sender, TappedRoutedEventArgs e)
        {
            ApplyEditable();
        }
    }
}
