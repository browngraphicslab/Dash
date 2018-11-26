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
        private bool _first = false;
        public TouchContextMenu()
        {
            this.InitializeComponent();
            Visibility = Visibility.Collapsed;
            HideMenuNoAsync();
            xRadialMenu.CenterButtonFontSize = 25;
        }

        public void ShowMenu(Point location, DocumentView docView)
        {
            _doc = docView;
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
            //stop drag?
            _doc?.DeleteDocument();
        }

        private void XCopy_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            _doc?.CopyDocument();
        }

        private void XKVP_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            Point pos = TransformToVisual(TouchInteractions.HeldDocument.GetFirstAncestorOfType<Canvas>()).TransformPoint(new Point(0, ActualHeight + 1));
           _doc?.ParentCollection?.ViewModel.AddDocument(TouchInteractions.HeldDocument.ViewModel.DocumentController.GetKeyValueAlias(pos));
        }

        //TODO: MAKE THESE INTO SUBMENU 

        private void XInstance_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            _doc?.MakeInstance();
        }

        private void XPin_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                if (_doc != null) MainPage.Instance.PinToPresentation(_doc.ViewModel.DocumentController);
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

        //if we want a size gauge
        private void XSize_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            
        }
    }
}
