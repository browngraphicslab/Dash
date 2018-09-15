using System;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Diagnostics;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionFreeformView
    {
        public CollectionFreeformView()
        {
            InitializeComponent();

            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoad;
            xOuterGrid.PointerEntered += OnPointerEntered;
            xOuterGrid.PointerExited += OnPointerExited;
            xOuterGrid.SizeChanged += OnSizeChanged;
            xOuterGrid.PointerPressed += OnPointerPressed;
            xOuterGrid.PointerReleased += OnPointerReleased;
            ViewManipulationControls = new ViewManipulationControls(this);
            ViewManipulationControls.OnManipulatorTranslatedOrScaled += ManipulationControls_OnManipulatorTranslated;

            _scaleX = 1.01;
            _scaleY = 1.01;
        }
        ~CollectionFreeformView()
        {
            Debug.WriteLine("FINALIZING CollectionFreeFormView");
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
        }

        public override Panel GetCanvas()
        {
            return xItemsControl.ItemsPanelRoot as Panel;
        }

        public override DocumentView ParentDocument => this.GetFirstAncestorOfType<DocumentView>();
        public override ViewManipulationControls ViewManipulationControls { get; set; }

        public override CollectionViewModel ViewModel => DataContext as CollectionViewModel;

        public override CollectionView.CollectionViewType Type => CollectionView.CollectionViewType.Freeform;

        public override ItemsControl GetItemsControl()
        {
            return xItemsControl;
        }

        public override ContentPresenter GetBackgroundContentPresenter()
        {
            return xBackgroundContentPresenter;
        }

        public override Grid GetOuterGrid()
        {
            return xOuterGrid;
        }

        public override AutoSuggestBox GetTagBox()
        {
            return TagKeyBox;
        }

        public override Canvas GetSelectionCanvas()
        {
            return SelectionCanvas;
        }

        public override Rectangle GetDropIndicationRectangle()
        {
            return XDropIndicationRectangle;
        }

        public override Canvas GetInkHostCanvas()
        {
            return InkHostCanvas;
        }

        private double _scaleX;
        private double _scaleY;

        CoreCursor Arrow = new CoreCursor(CoreCursorType.Arrow, 1);
        private void xOuterGrid_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {

            if (!this.IsLeftBtnPressed() && !this.IsRightBtnPressed())
            {
                Window.Current.CoreWindow.PointerCursor = Arrow;

                e.Handled = true;
            }
        }

        private void XOuterGrid_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Add && this.IsCtrlPressed())
            {
                _scaleX += 0.1;
                _scaleY += 0.1;
                var scaleDelta = new ScaleTransform
                {
                    CenterX = xOuterGrid.ActualWidth / 2,
                    CenterY = xOuterGrid.ActualHeight / 2,
                    ScaleX = _scaleX,
                    ScaleY = _scaleY
                };

                var composite = new TransformGroup();
                
                composite.Children.Add(xOuterGrid.RenderTransform); // get the current transform            
                composite.Children.Add(scaleDelta); // add the new scaling
                var matrix = composite.Value;
                ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
                MainPage.Instance.XDocumentDecorations.SetPositionAndSize(); // bcz: hack ... The Decorations should update automatically when the view zooms -- need a mechanism to bind/listen to view changing globally?
            }

            if (e.Key == VirtualKey.Subtract && this.IsCtrlPressed())
            {
                _scaleX -= 0.1;
                _scaleY -= 0.1;
                var scaleDelta = new ScaleTransform
                    {
                        CenterX = xOuterGrid.ActualWidth / 2,
                        CenterY = xOuterGrid.ActualHeight / 2,
                        ScaleX = _scaleX,
                        ScaleY = _scaleY
                    };

                    var composite = new TransformGroup();

                    composite.Children.Add(xOuterGrid.RenderTransform); // get the current transform            
                    composite.Children.Add(scaleDelta); // add the new scaling
                    var matrix = composite.Value;
                    ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
                    MainPage.Instance.XDocumentDecorations.SetPositionAndSize(); // bcz: hack ... The Decorations should update automatically when the view zooms -- need a mechanism to bind/listen to view changing globally?
              

            }

            if ((e.Key == VirtualKey.NumberPad0 || e.Key == VirtualKey.Number0) && this.IsCtrlPressed())
            {
                var scaleDelta = new ScaleTransform
                {
                    CenterX = xOuterGrid.ActualWidth / 2,
                    CenterY = xOuterGrid.ActualHeight / 2,
                    ScaleX = 1.0,
                    ScaleY = 1.0
                };

                var composite = new TransformGroup();

                composite.Children.Add(xOuterGrid.RenderTransform); // get the current transform            
                composite.Children.Add(scaleDelta); // add the new scaling
                var matrix = composite.Value;
                ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
                MainPage.Instance.XDocumentDecorations.SetPositionAndSize(); // bcz: hack ... The Decorations should update automatically when the view zooms -- need a mechanism to bind/listen to view changing globally?
            }

        }
    }
}
