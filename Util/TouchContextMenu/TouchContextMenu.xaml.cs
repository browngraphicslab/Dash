using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Antlr4.Runtime.Misc;
using RadialMenuControl.Components;
using Point = Windows.Foundation.Point;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TouchContextMenu : UserControl
    {

        private bool _first = false;
        private bool _firstMarq = false;
        private CollectionFreeformView _marqCol;
        private IEnumerable<DocumentView> _marqueeDocs;
        private Size _marqueeSize;
        private Point _marqWhere;
        private ArrayList<RadialMenuButton> _marqBtns;
        private ArrayList<RadialMenuButton> _regBtns;
        public TouchContextMenu()
        {
            this.InitializeComponent();
            HideMenuNoAsync();

            Visibility = Visibility.Visible;
        }

        public void ShowMenu(Point location, CollectionFreeformView col = null)
        {
            //IF MARQUEE IS ACTIVE -> SHOW MARQUEE/COLLECTION MENU
            if (col != null)
            {
                if (col._marquee == null)
                {
                    return;
                }
                //select docs in marquee
                _marqCol = col;

                _marqWhere = Util.PointTransformFromVisual(new Point(Canvas.GetLeft(_marqCol._marquee), Canvas.GetTop(_marqCol._marquee)),
                    _marqCol.SelectionCanvas, _marqCol.GetItemsControl().ItemsPanelRoot);
                _marqueeSize = new Size(_marqCol._marquee.Marquee.Width, _marqCol._marquee.Marquee.Height);
                _marqueeDocs = col.DocsInMarquee(new Rect(_marqWhere, new Size(_marqCol.Width, _marqCol.Height)));
                if (_marqueeDocs.Any())
                {
                    SelectionManager.SelectDocuments(_marqueeDocs, this.IsShiftPressed());
                    Focus(FocusState.Programmatic);
                }

                //xMarqueeMenu.Buttons.Clear();
                xMarqueeMenu.CenterButtonLeft = 0;
                Canvas.SetLeft(xMarqueeMenu.Pie, 0);
                Canvas.SetTop(xMarqueeMenu.Pie, 0);
                Canvas.SetLeft(this, location.X - 125);
                Canvas.SetTop(this, location.Y - 125);
               if (xMarqueeMenu.Pie.Visibility == Visibility.Collapsed) xMarqueeMenu.TogglePie();
                Visibility = Visibility.Visible;
                xMarqueeMenu.Visibility = Visibility.Visible;
                xRadialMenu.Visibility = Visibility.Collapsed;

                //formatting fixes to combat weird UWP bugs
                if (!_firstMarq)
                {
                    Canvas.SetTop(xMarqueeMenu.Pie, 30);
                    Canvas.SetLeft(xMarqueeMenu.Pie, 30);
                    _firstMarq = true;
                }

            }
            else
            {
                _marqCol = null;

                xRadialMenu.CenterButtonLeft = 0;
                Canvas.SetLeft(xRadialMenu.Pie, 0);
                Canvas.SetTop(xRadialMenu.Pie, 0);
                Canvas.SetLeft(this, location.X - 125);
                Canvas.SetTop(this, location.Y - 125);
                if (xRadialMenu.Pie.Visibility == Visibility.Collapsed) xRadialMenu.TogglePie();
               
                xRadialMenu.Buttons.Clear();
                xRadialMenu.Buttons.Add(xPin);
                Visibility = Visibility.Visible;
                xRadialMenu.Visibility = Visibility.Visible;
                xMarqueeMenu.Visibility = Visibility.Collapsed;

                //formatting fixes to combat weird UWP bugs
                if (!_first)
                {
                    Canvas.SetTop(xRadialMenu.Pie, 30);
                    Canvas.SetLeft(xRadialMenu.Pie, 30);
                    _first = true;
                }
            }



        }

        public async void HideMenuAsync()
        {
            if (xRadialMenu.Pie.Visibility == Visibility.Visible)
            {
                xRadialMenu.TogglePie();
                await Task.Delay(175);
            }
            if (xMarqueeMenu.Pie.Visibility == Visibility.Visible)
            {
                xMarqueeMenu.TogglePie();
                await Task.Delay(175);
            }
            Visibility = Visibility.Collapsed;
        }
        public void HideMenuNoAsync()
        {
            if (xRadialMenu.Pie.Visibility == Visibility.Visible) xRadialMenu.TogglePie();
            if (xMarqueeMenu.Pie.Visibility == Visibility.Visible) xMarqueeMenu.TogglePie();
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

        //ADD MARQUEE SPECIFIC BTNS HERE!

        private void XFreeform_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                //TODO: create collection from _marqueeDocs
                ArrayList<DocumentController> docs = new ArrayList<DocumentController>();
                ArrayList<DocumentViewModel> documentViewModels = new ArrayList<DocumentViewModel>();
                foreach (DocumentView view in _marqueeDocs)
                {
                    docs.Add(view.ViewModel.DocumentController);
                    documentViewModels.Add(view.ViewModel);
                }

                //create collecion with these docs
                _marqCol.ViewModel.AddDocument(new CollectionNote(_marqWhere, CollectionViewType.Freeform, _marqueeSize.Width, _marqueeSize.Height, docs).Document);

                SelectionManager.DeselectAll();

                foreach (var viewModel in documentViewModels)
                {
                    viewModel.RequestDelete();
                }
            }
        }

        private void XGrid_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                //TODO: create collection from _marqueeDocs
                ArrayList<DocumentController> docs = new ArrayList<DocumentController>();
                ArrayList<DocumentViewModel> documentViewModels = new ArrayList<DocumentViewModel>();
                foreach (DocumentView view in _marqueeDocs)
                {
                    docs.Add(view.ViewModel.DocumentController);
                    documentViewModels.Add(view.ViewModel);
                }

                //create collecion with these docs
                _marqCol.ViewModel.AddDocument(new CollectionNote(_marqWhere, CollectionViewType.Grid, _marqueeSize.Width, _marqueeSize.Height, docs).Document);

                SelectionManager.DeselectAll();

                foreach (var viewModel in documentViewModels)
                {
                    viewModel.RequestDelete();
                }
            }
        }

        private void XPage_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                //TODO: create collection from _marqueeDocs
                ArrayList<DocumentController> docs = new ArrayList<DocumentController>();
                ArrayList<DocumentViewModel> documentViewModels = new ArrayList<DocumentViewModel>();
                foreach (DocumentView view in _marqueeDocs)
                {
                    docs.Add(view.ViewModel.DocumentController);
                    documentViewModels.Add(view.ViewModel);
                }

                //create collecion with these docs
                _marqCol.ViewModel.AddDocument(new CollectionNote(_marqWhere, CollectionViewType.Page, _marqueeSize.Width, _marqueeSize.Height, docs).Document);

                SelectionManager.DeselectAll();

                foreach (var viewModel in documentViewModels)
                {
                    viewModel.RequestDelete();
                }
            }
        }

        private void XTimeline_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                //TODO: create collection from _marqueeDocs
                ArrayList<DocumentController> docs = new ArrayList<DocumentController>();
                ArrayList<DocumentViewModel> documentViewModels = new ArrayList<DocumentViewModel>();
                foreach (DocumentView view in _marqueeDocs)
                {
                    docs.Add(view.ViewModel.DocumentController);
                    documentViewModels.Add(view.ViewModel);
                }

                //create collecion with these docs
                _marqCol.ViewModel.AddDocument(new CollectionNote(_marqWhere, CollectionViewType.Timeline, _marqueeSize.Width, _marqueeSize.Height, docs).Document);

                SelectionManager.DeselectAll();

                foreach (var viewModel in documentViewModels)
                {
                    viewModel.RequestDelete();
                }
            }
        }

        private void XTreeView_OnInnerArcPressed(object sender, PointerRoutedEventArgs e)
        {
            using (UndoManager.GetBatchHandle())
            {
                //TODO: create collection from _marqueeDocs
                ArrayList<DocumentController> docs = new ArrayList<DocumentController>();
                ArrayList<DocumentViewModel> documentViewModels = new ArrayList<DocumentViewModel>();
                foreach (DocumentView view in _marqueeDocs)
                {
                    docs.Add(view.ViewModel.DocumentController);
                    documentViewModels.Add(view.ViewModel);
                }

                //create collecion with these docs
                _marqCol.ViewModel.AddDocument(new CollectionNote(_marqWhere, CollectionViewType.TreeView, _marqueeSize.Width, _marqueeSize.Height, docs).Document);

                SelectionManager.DeselectAll();

                foreach (var viewModel in documentViewModels)
                {
                    viewModel.RequestDelete();
                }
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

        private void OnCenterButtonTapped(object sender, TappedRoutedEventArgs e)
        {
            HideMenuAsync();
        }
    }
}
