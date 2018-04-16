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

namespace Dash.Views.Document_Menu
{
    public sealed partial class MenuToolBar : UserControl
    {
        private Canvas _parentCanvas;

        public MenuToolBar(Canvas canvas)
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
    }
}
