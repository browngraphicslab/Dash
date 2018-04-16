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
           // AddButton("Back", Symbol.Back, 1);
        }

        public void SetMenuToolBarBinding(RichEditBox selection)
        {
            DashMenuToolBar.Editor = selection;
        }

        public void AddButton(String name, Symbol icon, int position)
        {
            ToolbarButton button = DashMenuToolBar.GetDefaultButton(ButtonType.Headers);
            button.Visibility = Visibility.Collapsed;
            DashMenuToolBar.CustomButtons.Add(new ToolbarButton
                {
                Name = name,
                Icon = new SymbolIcon(icon),
                Position = position
                }
                );
            DashMenuToolBar.CustomButtons.Add(new ToolbarSeparator {Position = position + 1});
        }

        public void SetKeyboardShortcut()
        {

        }

        /// <summary>
        /// Updates the toolbar with the data from the current selected. TODO: bindings with this to MainPage.SelectedDocs?
        /// </summary>
        /// <param name="docs"></param>
        public void Update(IEnumerable<DocumentView> docs)
        {
            SetMenuToolBarBinding(VisualTreeHelperExtensions.GetFirstDescendantOfType<RichEditBox>(docs.First()));
        }

        private void SetUpBaseMenu()
        {
            _parentCanvas.Children.Add(this);
            Canvas.SetLeft(this, 325);
            Canvas.SetTop(this, 5);
            Debug.WriteLine(_parentCanvas.Width);
            Debug.WriteLine(_parentCanvas.Height);
            Debug.WriteLine("TOOLBAR LOADED!");
            //SetDefaultMenuStyle();
            //SetButtonActions();
        }
    }
}
