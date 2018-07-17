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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class PdfSubToolbar : UserControl, ICommandBarBased
    {
        private DocumentView _currentDocView;
        private CustomPdfView _currentPdfView;
        private DocumentController _currentDocController;

        public PdfSubToolbar()
        {
            this.InitializeComponent();
            Visibility = Visibility.Collapsed;
        }

	    private void ToggleAnnotations_Checked(object sender, RoutedEventArgs e)
	    {
		   // xPdfCommandbar.IsOpen = true;
			_currentPdfView?.AnnotationManager.ShowRegions();
		    xToggleAnnotations.Label = "Hide";
	
	    }

	    private void ToggleAnnotations_Unchecked(object sender, RoutedEventArgs e)
	    {
		   // xPdfCommandbar.IsOpen = true;
			_currentPdfView?.AnnotationManager.HideRegions();
		    xToggleAnnotations.Label = "Show";
	    }

        /// <summary>
        /// Enables the subtoolbar access to the Document View of the image that was selected on tap.
        /// </summary>
        internal void SetPdfBinding(DocumentView selection)
        {
            _currentDocView = selection;
            _currentPdfView = _currentDocView.GetFirstDescendantOfType<CustomPdfView>();
            _currentDocController = _currentDocView.ViewModel.DocumentController;
        }

        private void XInkToggle_OnChecked(object sender, RoutedEventArgs e)
        {
            xRegionToggle.IsChecked = false;
            xTextToggle.IsChecked = false;

           // xPdfCommandbar.IsOpen = true;
           // xPdfCommandbar.IsEnabled = true;
            _currentPdfView.AnnotationManager.CurrentAnnotationType = AnnotationManager.AnnotationType.Ink;
        }

        private void XTextToggle_OnChecked(object sender, RoutedEventArgs e)
        {
            xRegionToggle.IsChecked = false;
            xInkToggle.IsChecked = false;

           // xPdfCommandbar.IsOpen = true;
           // xPdfCommandbar.IsEnabled = true;
            _currentPdfView.AnnotationManager.CurrentAnnotationType = AnnotationManager.AnnotationType.TextSelection;
        }

        private void XRegionToggle_OnChecked(object sender, RoutedEventArgs e)
        {
            xInkToggle.IsChecked = false;
            xTextToggle.IsChecked = false;

           // xPdfCommandbar.IsOpen = true;
          //  xPdfCommandbar.IsEnabled = true;
            _currentPdfView.AnnotationManager.CurrentAnnotationType = AnnotationManager.AnnotationType.RegionBox;
        }

        public void CommandBarOpen(bool status)
        {
           // xPdfCommandbar.IsOpen = status;
          //  xPdfCommandbar.IsEnabled = true;
            xPdfCommandbar.Visibility = Visibility.Visible;
        }

        public void SetComboBoxVisibility(Visibility visibility)
        {
        }

	    private void XPdfCommandbar_OnPointerPressed(object sender, PointerRoutedEventArgs e)
	    {
		    //xPdfCommandbar.IsOpen = true;
	    }

	    private void XInkToggle_OnUnchecked(object sender, RoutedEventArgs e)
	    {
			//xPdfCommandbar.IsOpen = true;
		}

	    private void XTextToggle_OnUnchecked(object sender, RoutedEventArgs e)
	    {
		    //xPdfCommandbar.IsOpen = true;
	    }

	    private void XRegionToggle_OnUnchecked(object sender, RoutedEventArgs e)
	    {
			//xPdfCommandbar.IsOpen = true;
		}
    }
}
