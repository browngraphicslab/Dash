using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Point = Windows.Foundation.Point;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TouchContextMenu : UserControl
    {
        //this can be replaced with TouchInteractions.HeldDocument
        private DocumentView _doc;
        public TouchContextMenu()
        {
            this.InitializeComponent();
            HideMenuNoAsync();
        }

        public void InitializeMenu(Point location, DocumentView docView)
        {
            //if (_doc == docView) await HideMenuAsync();
            _doc = docView;
            Canvas.SetLeft(this, location.X - ActualWidth / 2);
            Canvas.SetTop(this, location.Y - ActualHeight / 2);
            if (xRadialMenu.Pie.Visibility == Visibility.Collapsed) xRadialMenu.TogglePie();
            Visibility = Visibility.Visible;
        }

        public async System.Threading.Tasks.Task HideMenuAsync()
        {
            _doc = null;
            if (xRadialMenu.Pie.Visibility == Visibility.Visible)
            {
                xRadialMenu.TogglePie();
                await Task.Delay(175);
            }
            Visibility = Visibility.Collapsed;
        }
        public void HideMenuNoAsync()
        {
            _doc = null;
            if (xRadialMenu.Pie.Visibility == Visibility.Visible) xRadialMenu.TogglePie();
            Visibility = Visibility.Collapsed;
        }

        public async System.Threading.Tasks.Task CloseMenuAsync()
        {
            if (xRadialMenu.Pie.Visibility == Visibility.Visible)
            {
                xRadialMenu.TogglePie();
                await Task.Delay(175);
            }
        }

        private void XDelete_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            //stop drag
            _doc.DeleteDocument();
        }

        private void XCopy_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            _doc.CopyDocument();
        }
    }
}
