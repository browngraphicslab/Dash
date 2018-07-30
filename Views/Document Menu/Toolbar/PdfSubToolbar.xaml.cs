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
        private DispatcherTimer _textboxTimer;

        public PdfSubToolbar()
        {
            this.InitializeComponent();
            Visibility = Visibility.Collapsed;
            _textboxTimer = new DispatcherTimer()
            {
                Interval = new TimeSpan(0, 0, 0, 0, 700)
            };
            _textboxTimer.Tick += TimerTick;
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
                case AnnotationType.Pin:
                    xPinToggle.IsChecked = true;
                    break;
                default:
                    xPinToggle.IsChecked = false;
                    xInkToggle.IsChecked = false;
                    xTextToggle.IsChecked = false;
                    xRegionToggle.IsChecked = false;
                    break;
            }
        }

        private void XInkToggle_OnChecked(object sender, RoutedEventArgs e)
        {
            xRegionToggle.IsChecked = false;
            xTextToggle.IsChecked = false;
            xPinToggle.IsChecked = false;

            _currentPdfView.SetAnnotationType(AnnotationType.Ink);
        }

        private void XTextToggle_OnChecked(object sender, RoutedEventArgs e)
        {
            xRegionToggle.IsChecked = false;
            xInkToggle.IsChecked = false;
            xPinToggle.IsChecked = false;

            _currentPdfView.SetAnnotationType(AnnotationType.Selection);
        }

        private void XRegionToggle_OnChecked(object sender, RoutedEventArgs e)
        {
            xInkToggle.IsChecked = false;
            xTextToggle.IsChecked = false;
            xPinToggle.IsChecked = false;

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
            if (xInkToggle.IsChecked == false && xTextToggle.IsChecked == false && xRegionToggle.IsChecked == false && xPinToggle.IsChecked == false)
            {
                _currentPdfView.SetAnnotationType(AnnotationType.None);
            }
        }

        private void XPinToggle_OnChecked_(object sender, RoutedEventArgs e)
        {
            xInkToggle.IsChecked = false;
            xTextToggle.IsChecked = false;
            xRegionToggle.IsChecked = false;

            _currentPdfView.SetAnnotationType(AnnotationType.Pin);
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
        private void XToPage_OnTextChanged(object sender, TextChangedEventArgs textChangedEventArgs)
        {


            _textboxTimer.Stop();
            _textboxTimer.Start();


        }

        private void TimerTick(object sender, object e)
        {

            _textboxTimer.Stop();
            var desiredPage = xToPageBox.Text;

            if (!double.TryParse(desiredPage, out double pageNum))
            {
                return;
            }
            if (pageNum > _currentPdfView.BottomPages.PageSizes.Count)
            {
                return;
            }

            _currentPdfView.GoToPage(pageNum);
        }
    }
}
