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
using Microsoft.Toolkit.Uwp.UI.Controls.TextToolbarButtons;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System.Diagnostics;


// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    /// <summary>
    /// The top level toolbar in Dash.
    /// </summary>
    public sealed partial class MenuToolbar : UserControl
    {
        // == FIELDS
        private UIElement subtoolbarElement = null; // currently active submenu, if null, nothing is selected
        private AppBarButton[] docSpecificButtons;
        private Canvas _parentCanvas;

        // == CONSTRUCTORS ==
        /// <summary>
        /// Creates a new Toolbar with the given canvas as reference.
        /// </summary>
        /// <param name="canvas"></param>
        public MenuToolbar(Canvas canvas)
        {
            this.InitializeComponent();
            _parentCanvas = canvas;

            // list of buttons that are enabled only if there is 1 or more selected documents
            AppBarButton[] buttons = { xCopy, xDelete };
            docSpecificButtons = buttons;
            this.SetUpBaseMenu();
        }

        // == METHODS ==
        /// <summary>
        /// Disables or enables toolbar level document specific icons.
        /// </summary>
        /// <param name="hasDocuments"></param>
        private void toggleSelectOptions(Boolean hasDocuments) {
            var o = .5;
            if (hasDocuments) o = 1;
            foreach (AppBarButton b in docSpecificButtons)
            {
                b.IsEnabled = hasDocuments;
                b.Opacity = o;
            }
        }

        /// <summary>
        /// Updates the toolbar with the data from the current selected. TODO: bindings with this to MainPage.SelectedDocs?
        /// </summary>
        /// <param name="docs"></param>
        public void Update(IEnumerable<DocumentView> docs)
        {
            if (subtoolbarElement != null) subtoolbarElement.Visibility = Visibility.Collapsed;

            toggleSelectOptions(docs.Count<DocumentView>() > 0);

            // just single select
            if (docs.Count<DocumentView>() == 1)
            {
                // Text controls
                var text = VisualTreeHelperExtensions.GetFirstDescendantOfType<RichEditBox>(docs.First());
                if (text != null)
                {
                    xTextToolbar.SetMenuToolBarBinding(VisualTreeHelperExtensions.GetFirstDescendantOfType<RichEditBox>(docs.First()));
                    subtoolbarElement = xTextToolbar;
                }

				// TODO: Image controls
				var image = VisualTreeHelperExtensions.GetFirstDescendantOfType<Image>(docs.First());
				if (image != null)
				{
					subtoolbarElement = xImageToolbar;
				}

                // TODO: Collection controls  
                
                var col = VisualTreeHelperExtensions.GetFirstDescendantOfType<CollectionView>(docs.First());
                if (col != null)
                {
                    CollectionView thisCollection = VisualTreeHelperExtensions.GetFirstDescendantOfType<CollectionView>(docs.First());
                    subtoolbarElement = xCollectionToolbar;
                }
            }
            else if (docs.Count<DocumentView>() > 1)
            {
                // TODO: multi select
            }
            else {
                subtoolbarElement = null;
            }
            if (subtoolbarElement != null) subtoolbarElement.Visibility = Visibility.Visible;
        }

        private void SetUpBaseMenu()
        {
            _parentCanvas.Children.Add(this);
            Canvas.SetLeft(this, 325);
            Canvas.SetTop(this, 5);
        }

        // moves toolbar on drag TODO: merge w/ docking code
        private void UIElement_OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var newLatPo = xToolbarTransform.TranslateX + e.Delta.Translation.X;
            var newVertPo = xToolbarTransform.TranslateX + e.Delta.Translation.Y;
            var actualWidth = ((Frame) Window.Current.Content).ActualWidth;
            var actualHeight = ((Frame)Window.Current.Content).ActualHeight;
            if (newLatPo > 0 && newLatPo < actualWidth)
            {
                xToolbarTransform.TranslateX += e.Delta.Translation.X;
            }
            if (newVertPo > 0 && newVertPo < actualHeight)
            {
                xToolbarTransform.TranslateY += e.Delta.Translation.Y;
            }
        }

        // copy btn
        private void Copy(object sender, RoutedEventArgs e)
        {
            foreach (DocumentView d in MainPage.Instance.GetSelectedDocuments()) {
                d.CopyDocument();
            }  
        }

        // delete btn
        private void Delete(object sender, RoutedEventArgs e)
        {
            foreach (DocumentView d in MainPage.Instance.GetSelectedDocuments())
            {
                d.DeleteDocument();
            }
        }
    }
}
