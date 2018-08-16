using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
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
            SetUpToolTips();

        }

        private void ToggleAnnotations_Checked(object sender, RoutedEventArgs e)
	    {
            // xPdfCommandbar.IsOpen = true;
            _currentPdfView?.ShowRegions();
            xToggleAnnotations.Label = "Visible";
	    }

	    private void ToggleAnnotations_Unchecked(object sender, RoutedEventArgs e)
	    {
            // xPdfCommandbar.IsOpen = true;
            _currentPdfView?.HideRegions();
            xToggleAnnotations.Label = "Hidden";
	    }

        /// <summary>
        /// Enables the subtoolbar access to the Document View of the image that was selected on tap.
        /// </summary>
        internal void SetPdfBinding(DocumentView selection)
        {
            _currentDocView = selection;
            _currentPdfView = _currentDocView.GetFirstDescendantOfType<CustomPdfView>();
            _currentDocController = _currentDocView.ViewModel.DocumentController;

            xToggleAnnotations.IsChecked = _currentPdfView.AreAnnotationsVisible();

            //update selected annotation type according to this newly selected PDF
            switch (_currentPdfView.CurrentAnnotationType)
            {
                case AnnotationType.Ink:
                    xInkToggle.IsChecked = true;
                    break;
                case AnnotationType.Selection:
                    xTextToggle.IsChecked = true;
                    break;
                case AnnotationType.Region:
                    xRegionToggle.IsChecked = true;
                    break;
                default:
                    xInkToggle.IsChecked = false;
                    xTextToggle.IsChecked = false;
                    xRegionToggle.IsChecked = false;
                    break;
            }
        }

        public void Update(AnnotationType type)
        {
            xInkToggle.IsChecked = type == AnnotationType.Ink;
            xTextToggle.IsChecked = type == AnnotationType.Selection;
            xRegionToggle.IsChecked = type == AnnotationType.Region;

        }

        private void XInkToggle_OnChecked(object sender, RoutedEventArgs e)
        {
            xRegionToggle.IsChecked = false;
            xTextToggle.IsChecked = false;

            _currentPdfView.SetAnnotationType(AnnotationType.Ink);
        }

        private void XTextToggle_OnChecked(object sender, RoutedEventArgs e)
        {
            xRegionToggle.IsChecked = false;
            xInkToggle.IsChecked = false;

            _currentPdfView.SetAnnotationType(AnnotationType.Selection);
        }

        private void XRegionToggle_OnChecked(object sender, RoutedEventArgs e)
        {
            xInkToggle.IsChecked = false;
            xTextToggle.IsChecked = false;

            _currentPdfView.SetAnnotationType(AnnotationType.Region);
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

        private void Toggle_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (xInkToggle.IsChecked == false && xTextToggle.IsChecked == false && xRegionToggle.IsChecked == false )
            {
                _currentPdfView.SetAnnotationType(AnnotationType.None);
            }
        }

        //private void XFontSizeTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        //{
        //    
        //        var selectedFontSize = xFontSizeTextBox.Text;

        //        if (!double.TryParse(selectedFontSize, out double fontSize))
        //        {
        //            return;
        //        }
        //        if (fontSize > 1600)
        //        {
        //            return;
        //        }
        //        using (UndoManager.GetBatchHandle())
        //        {
        //            if (xRichEditBox.Document.Selection == null || xRichEditBox.Document.Selection.StartPosition ==
        //                xRichEditBox.Document.Selection.EndPosition)
        //            {
        //                xRichEditBox.Document.GetText(TextGetOptions.UseObjectText, out var text);
        //                var end = text.Length;
        //                xRichEditBox.Document.Selection.SetRange(0, end);
        //                xRichEditBox.Document.Selection.CharacterFormat.Size = (float)fontSize;
        //                xRichEditBox.Document.Selection.SetRange(end, end);
        //            }
        //            else
        //            {
        //                xRichEditBox.Document.Selection.CharacterFormat.Size = (float)fontSize;
        //            }

        //            richTextView.UpdateDocumentFromXaml();
        //        }
        //    
        //}
      

        private void XToPageBox_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
	            var desiredPage = xToPageBox.Text;

		            if (!double.TryParse(desiredPage, out double pageNum))
		            {
			            xToPageBox.PlaceholderText = "Error: invalid page #";
			            xToPageBox.Text = "";
			            //xFadeAnimationIn.Begin();
			            //xFadeAnimationOut.Begin();
			            return;
		            }
		            if (pageNum > _currentPdfView.BottomPages.PageSizes.Count)
		            {
			            xToPageBox.PlaceholderText = "Error: invalid page #";
			            xToPageBox.Text = "";
			            //xFadeAnimationIn.Begin();
			            //xFadeAnimationOut.Begin();
			            return;
		            }

		            _currentPdfView.GoToPage(pageNum);
		            xToPageBox.PlaceholderText = "Go to page...";
				}
        }

        private ToolTip _toggle;
	    private ToolTip _scrollVis;
        private ToolTip _ink;
        private ToolTip _text;
        private ToolTip _region;

        private void SetUpToolTips()
        {
            const PlacementMode placementMode = PlacementMode.Bottom;
            const int offset = 5;

            _toggle = new ToolTip()
            {
                Content = "Toggle Annotations",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xToggleAnnotations, _toggle);

	        _scrollVis = new ToolTip()
	        {
		        Content = "Annotations Visible on Scroll",
		        Placement = placementMode,
		        VerticalOffset = offset
	        };
	        ToolTipService.SetToolTip(xAnnotationsVisibleOnScroll, _scrollVis);

			_ink = new ToolTip()
            {
                Content = "Ink Annotation",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xInkToggle, _ink);

            _text = new ToolTip()
            {
                Content = "Text Annotation",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xTextToggle, _text);

          
            _region = new ToolTip()
            {
                Content = "Region Annotation",
                Placement = placementMode,
                VerticalOffset = offset
            };
            ToolTipService.SetToolTip(xRegionToggle, _region);            
        }

        private void ShowAppBarToolTip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is AppBarButton button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = true;
            else if (sender is AppBarToggleButton toggleButton && ToolTipService.GetToolTip(toggleButton) is ToolTip toggleTip) toggleTip.IsOpen = true;
        }

        private void HideAppBarToolTip(object sender, PointerRoutedEventArgs e)
        {
            if (sender is AppBarButton button && ToolTipService.GetToolTip(button) is ToolTip tip) tip.IsOpen = false;
            else if (sender is AppBarToggleButton toggleButton && ToolTipService.GetToolTip(toggleButton) is ToolTip toggleTip) toggleTip.IsOpen = false;
        }

	    private void XAnnotationsVisibleOnScroll_OnChecked(object sender, RoutedEventArgs e)
	    {
		    _currentPdfView.SetAnnotationsVisibleOnScroll(true);
	    }

	    private void XAnnotationsVisibleOnScroll_OnUnchecked(object sender, RoutedEventArgs e)
	    {
		    _currentPdfView.SetAnnotationsVisibleOnScroll(false);
	    }
	}


}
