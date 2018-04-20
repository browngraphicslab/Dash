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
    public sealed partial class MenuToolbar : UserControl
    {
        private Canvas _parentCanvas;

        public MenuToolbar(Canvas canvas)
        {
            this.InitializeComponent();
            _parentCanvas = canvas;
            this.SetUpBaseMenu();
        }

        public void SetKeyboardShortcut()
        {

        }

        UIElement subtoolbarElement = null; // currently active submenu, if null, nothing is selected

        /// <summary>
        /// Updates the toolbar with the data from the current selected. TODO: bindings with this to MainPage.SelectedDocs?
        /// </summary>
        /// <param name="docs"></param>
        public void Update(IEnumerable<DocumentView> docs)
        {
            if (subtoolbarElement != null) subtoolbarElement.Visibility = Visibility.Collapsed;

            // just single select
            if (docs.Count<DocumentView>() == 1)
            {
                // Text controls
                var text = VisualTreeHelperExtensions.GetFirstDescendantOfType<RichEditBox>(docs.First());
                if (text != null)
                {
                    xTextToolbar.SetMenuToolBarBinding(VisualTreeHelperExtensions.GetFirstDescendantOfType<RichEditBox>(docs.First()));
                    subtoolbarElement = xTextToolbar;
                    return;
                }

                // TODO: Image controls

                // TODO: Collection controls  
                
                var col = VisualTreeHelperExtensions.GetFirstDescendantOfType<CollectionView>(docs.First());
                if (col != null)
                {
                    CollectionView thisCollection = VisualTreeHelperExtensions.GetFirstDescendantOfType<CollectionView>(docs.First());
                    subtoolbarElement = xCollectionToolbar;
                    return;
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
    }
}
