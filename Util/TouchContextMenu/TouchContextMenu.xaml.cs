using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.UI;
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

        private bool _first = false;
        public TouchContextMenu()
        {
            this.InitializeComponent();
            Visibility = Visibility.Collapsed;
            HideMenuNoAsync();
            xRadialMenu.CenterButtonFontSize = 25;
        }

        public void ShowMenu(Point location)
        {
            xRadialMenu.CenterButtonLeft = 0;
            Canvas.SetLeft(xRadialMenu.Pie, 0);
            Canvas.SetTop(xRadialMenu.Pie, 0);
            Canvas.SetLeft(this, location.X - 125);
            Canvas.SetTop(this, location.Y - 125);
            if (xRadialMenu.Pie.Visibility == Visibility.Collapsed) xRadialMenu.TogglePie();
            Visibility = Visibility.Visible;

            //formatting fixes to combat weird UWP bugs
            if (!_first)
            {
                Canvas.SetTop(xRadialMenu.Pie, 30);
                Canvas.SetLeft(xRadialMenu.Pie, 30);
                _first = true;
            }
        }

        public async void HideMenuAsync()
        {
            if (xRadialMenu.Pie.Visibility == Visibility.Visible)
            {
                xRadialMenu.TogglePie();
                await Task.Delay(175);
            }
            Visibility = Visibility.Collapsed;
        }
        public void HideMenuNoAsync()
        {
            if (xRadialMenu.Pie.Visibility == Visibility.Visible) xRadialMenu.TogglePie();
            Visibility = Visibility.Collapsed;
        }

        private void XDelete_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            //TODO:stop drag?
            TouchInteractions.HeldDocument?.DeleteDocument();
        }

        private void XCopy_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            //TODO: MIGHT HAVE TO STOP DRAG!
            Point pos = TransformToVisual(TouchInteractions.HeldDocument.GetFirstAncestorOfType<Canvas>()).TransformPoint(new Point(0, 0));
            TouchInteractions.HeldDocument?.CopyDocument(pos);
        }

        private void XKVP_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            Point pos = TransformToVisual(TouchInteractions.HeldDocument.GetFirstAncestorOfType<Canvas>()).TransformPoint(new Point(0, 0));
           TouchInteractions.HeldDocument?.ParentCollection?.ViewModel.AddDocument(TouchInteractions.HeldDocument.ViewModel.DocumentController.GetKeyValueAlias(pos));
        }

        //TODO: MAKE THESE INTO SUBMENU 

        private void XInstance_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            Point pos = TransformToVisual(TouchInteractions.HeldDocument.GetFirstAncestorOfType<Canvas>()).TransformPoint(new Point(0, 0));
            TouchInteractions.HeldDocument?.MakeInstance(pos);
        }

        private void XPin_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                if (TouchInteractions.HeldDocument != null) MainPage.Instance.xPresentationView.PinToPresentation(TouchInteractions.HeldDocument.ViewModel.DocumentController);
            }
        }

        /*
        private void XFitHeight_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            DocumentView d = _doc;
            if (d.ViewModel.LayoutDocument.GetVerticalAlignment() == VerticalAlignment.Stretch)
            {
                d.ViewModel.LayoutDocument.SetHeight(d.ViewModel.LayoutDocument.GetDereferencedField<NumberController>(KeyStore.CollectionOpenHeightKey, null)?.Data ??
                                                     (!double.IsNaN(d.ViewModel.LayoutDocument.GetHeight()) ? d.ViewModel.LayoutDocument.GetHeight() :
                                                         d.ViewModel.LayoutDocument.GetActualSize().Value.Y));
                d.ViewModel.LayoutDocument.SetVerticalAlignment(VerticalAlignment.Top);
            }
            else if (!(d.GetFirstAncestorOfType<CollectionView>()?.CurrentView is CollectionFreeformView))
            {
                d.ViewModel.LayoutDocument.SetField<NumberController>(KeyStore.CollectionOpenHeightKey, d.ViewModel.LayoutDocument.GetHeight(), true);
                d.ViewModel.LayoutDocument.SetHeight(double.NaN);
                d.ViewModel.LayoutDocument.SetVerticalAlignment(VerticalAlignment.Stretch);
            }
        }

        private void XFitWidth_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            DocumentView d = _doc;
            if (d.ViewModel.LayoutDocument.GetHorizontalAlignment() == HorizontalAlignment.Stretch)
            {
                d.ViewModel.LayoutDocument.SetWidth(d.ViewModel.LayoutDocument.GetDereferencedField<NumberController>(KeyStore.CollectionOpenWidthKey, null)?.Data ??
                                                    (!double.IsNaN(d.ViewModel.LayoutDocument.GetWidth()) ? d.ViewModel.LayoutDocument.GetWidth() :
                                                        d.ViewModel.LayoutDocument.GetActualSize().Value.X));
                d.ViewModel.LayoutDocument.SetHorizontalAlignment(HorizontalAlignment.Left);
            }
            else if (!(d.GetFirstAncestorOfType<CollectionView>()?.CurrentView is CollectionFreeformView))
            {
                d.ViewModel.LayoutDocument.SetField<NumberController>(KeyStore.CollectionOpenWidthKey, d.ViewModel.LayoutDocument.GetWidth(), true);
                d.ViewModel.LayoutDocument.SetWidth(double.NaN);
                d.ViewModel.LayoutDocument.SetHorizontalAlignment(HorizontalAlignment.Stretch);
            }
        }

    */

        private void TouchContextMenu_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            HideMenuAsync();
        }
    }
}
