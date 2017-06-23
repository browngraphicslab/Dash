using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionView : UserControl
    {

        public double CanvasScale { get; set; } = 1;
        public const float MaxScale = 10;
        public const float MinScale = 0.5f;
        public Rect Bounds = new Rect(0, 0, 5000, 5000);

        public CollectionViewModel ViewModel;
        private bool _isHasFieldPreviouslySelected;
        public Grid OuterGrid
        {
            get { return Grid; }
            set { Grid = value; }
        }

        public CollectionView(CollectionViewModel vm)
        {
            this.InitializeComponent();
            DataContext = ViewModel = vm;
            SetEventHandlers();
        }

        private void SetEventHandlers()
        {
            GridOption.Tapped += ViewModel.GridViewButton_Tapped;
            ListOption.Tapped += ViewModel.ListViewButton_Tapped;
            CloseButton.Tapped += CloseButton_Tapped;
            SelectButton.Tapped += ViewModel.SelectButton_Tapped;
            DeleteSelected.Tapped += ViewModel.DeleteSelected_Tapped;
            Filter.Tapped += ViewModel.FilterSelection_Tapped;
            ClearFilter.Tapped += ViewModel.ClearFilter_Tapped;
            //CancelSoloDisplayButton.Tapped += ViewModel.CancelSoloDisplayButton_Tapped;

            HListView.SelectionChanged += ViewModel.SelectionChanged;
            // GridView.SelectionChanged += ViewModel.SelectionChanged;
            

            

            Grid.DoubleTapped += ViewModel.OuterGrid_DoubleTapped;

            SingleDocDisplayGrid.Tapped += ViewModel.SingleDocDisplayGrid_Tapped;

            xFilterExit.Tapped += ViewModel.FilterExit_Tapped;
            xFilterButton.Tapped += ViewModel.FilterButton_Tapped;


            xSearchBox.TextCompositionEnded += ViewModel.SearchBox_TextEntered;
            xSearchBox.TextChanged += ViewModel.xSearchBox_TextChanged;

            xFieldBox.TextChanged += ViewModel.FilterFieldBox_OnTextChanged;
            xFieldBox.SuggestionChosen += ViewModel.FilterFieldBox_SuggestionChosen;
            xFieldBox.QuerySubmitted += ViewModel.FilterFieldBox_QuerySubmitted;
            
            xSearchFieldBox.TextChanged += ViewModel.xSearchFieldBox_TextChanged;

        }

        private void CloseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var canvas = Parent as Canvas;
            canvas?.Children.Remove(this);
        }

        private void UIElement_OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ViewModel.DocumentView_OnDoubleTapped(sender, e);
            e.Handled = true;
        }

        private void SoloDocument_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }

        private void Grid_OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (e.Container is ScrollBar || e.Container is ScrollViewer)
            {
                e.Complete();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Animate fadeout of the xFieldBox and the collapsing of the xMainGrid
        /// when the "Has field" option is selected in the combobox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hasField_Tapped(object sender, TappedRoutedEventArgs e)
        {
            _isHasFieldPreviouslySelected = true;

            ViewModel.CollectionFilterMode = CollectionViewModel.FilterMode.HasField;

            // collapse only if the grid that the xFieldBox is located in is expanded
            if (xFieldBoxColumn.Width > 0)
            {
                xHideFieldBox.Begin();
                xCollapseMainGrid.Begin();
            }

            xSearchBox.Visibility = Visibility.Collapsed;
            xSearchFieldBox.Visibility = Visibility.Visible;

            // case where xSearchBox is filled in before user clicks on xHasField
            if (xSearchFieldBox.Text != "")
            {
                xFilterButton.Visibility = Visibility.Visible;
            }

            if (xFieldBox.Text != "")
            {
                xSearchFieldBox.Text = xFieldBox.Text;
                xFieldBox.Text = "";
            }
        }
        /// <summary>
        /// Animate expansion of xMainGrid when the "Field contains" or "Field equals" option is
        /// selected in the combobox (and the previously selected option is "Has field")
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fieldContainsOrEquals_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // expand only if the grid that the xFieldBox is located in is collapsed
            if (xFieldBoxColumn.Width == 0)
            {
                // resize actual grid column
                xFieldBoxColumn.Width = 165;
                xExpandMainGrid.Begin();
            }

            xSearchBox.Visibility = Visibility.Visible;
            xSearchFieldBox.Visibility = Visibility.Collapsed;

            // xFieldBox is cleared when xFieldContains or xFieldEquals is selected, button must be disabled
            if (xFieldBox.Text == "")
            {
                xFilterButton.Visibility = Visibility.Collapsed;
                // case where field option is selected after the text boxes are filled in
            }
            else if (xFieldBox.Text != "" && xSearchBox.Text != "")
            {
                xFilterButton.Visibility = Visibility.Visible;
            }

            if (xSearchFieldBox.Text != "" && _isHasFieldPreviouslySelected)
            {
                xFieldBox.Text = xSearchFieldBox.Text;
                xSearchFieldBox.Text = "";
            }

            _isHasFieldPreviouslySelected = false;
        }

        /// <summary>
        /// Animate fadein of the xFieldBox when the animation that expands the xMainGrid finishes playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xExpandMainGrid_Completed(object sender, object e)
        {
            xShowFieldBox.Begin();
        }

        /// <summary>
        /// Ensure that the filter button is only responsive when all available combo and text boxes are filled in
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            EnableOrDisableFilterButton();
        }

        /// <summary>
        /// Generate autosuggestions according to available fields when user types into the autosuggestionbox to prevent mispelling
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>


        /// <summary>
        /// Specify conditions for the FILTER button to enable or disable
        /// </summary>
        private void EnableOrDisableFilterButton()
        {
            if (xComboBox.SelectedItem == xHasField && xSearchFieldBox.Text != "" || xComboBox.SelectedItem != xHasField && xComboBox.SelectedItem != null && xSearchBox.Text != "" && xFieldBox.Text != "")
            {
                xFilterButton.Visibility = Visibility.Visible;
            }
            else
            {
                xFilterButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Remove entire filter view from its parent when the animation finishes playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FadeOutThemeAnimation_Completed(object sender, object e)
        {
            ((Grid)this.Parent).Children.Remove(this);
        }

        /// <summary>
        /// Resize the grid column that the xFieldBox is located in when the animation that collapses
        /// the xMainGrid and fades out the xFieldBox finishes playing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void xCollapseMainGrid_Completed(object sender, object e)
        {
            xFieldBoxColumn.Width = 0;
        }

        private void XFieldBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            
            // enable and disable button accordingly
            EnableOrDisableFilterButton();
        }

        private void fieldContains_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.CollectionFilterMode = CollectionViewModel.FilterMode.FieldContains;
            fieldContainsOrEquals_Tapped(sender, e);
        }

        private void fieldEquals_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ViewModel.CollectionFilterMode = CollectionViewModel.FilterMode.FieldEquals;
            fieldContainsOrEquals_Tapped(sender, e);
        }
       



        private void DocumentView_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            var cvm = DataContext as CollectionViewModel;
            //(sender as DocumentView).Manipulator.TurnOff();

        }

        private void DocumentView_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var cvm = DataContext as CollectionViewModel;
            var dv  = (sender as DocumentView);
            var dvm = dv.DataContext as DocumentViewModel;
            cvm.MoveDocument(dvm, dv.RenderTransform.TransformPoint(new Point(e.Delta.Translation.X, e.Delta.Translation.Y)));
            e.Handled = true;
        }

        private void DocumentViewContainerGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ClipRect.Rect = new Rect(0,0, e.NewSize.Width, e.NewSize.Height);
        }

        /// <summary>
        /// Pans and zooms upon touch manipulation 
        /// </summary>
        private void UserControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            Canvas canvas = GridView.ItemsPanelRoot as Canvas;
            Debug.Assert(canvas != null);
            e.Handled = true;
            ManipulationDelta delta = e.Delta;

            //Create initial translate and scale transforms
            //Translate is in screen space, scale is in canvas space
            TranslateTransform translate = new TranslateTransform
            {
                X = delta.Translation.X,
                Y = delta.Translation.Y
            };

            Point p = Util.PointTransformFromVisual(e.Position, canvas);
            ScaleTransform scale = new ScaleTransform
            {
                CenterX = p.X,
                CenterY = p.Y,
                ScaleX = delta.Scale,
                ScaleY = delta.Scale
            };

            //Clamp the zoom
            CanvasScale *= delta.Scale;
            if (CanvasScale > MaxScale)
            {
                CanvasScale = MaxScale;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }
            if (CanvasScale < MinScale)
            {
                CanvasScale = MinScale;
                scale.ScaleX = 1;
                scale.ScaleY = 1;
            }

            //Create initial composite transform
            TransformGroup composite = new TransformGroup();
            composite.Children.Add(scale);
            composite.Children.Add(canvas.RenderTransform);
            composite.Children.Add(translate);

            //Get top left and bottom right screen space points in canvas space
            GeneralTransform inverse = composite.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(canvas.RenderTransform != null);
            GeneralTransform renderInverse = canvas.RenderTransform.Inverse;
            Debug.Assert(renderInverse != null);
            Point topLeft = inverse.TransformPoint(new Point(0, 0));
            Point bottomRight = inverse.TransformPoint(new Point(Grid.ActualWidth, Grid.ActualHeight));
            Point preTopLeft = renderInverse.TransformPoint(new Point(0, 0));
            Point preBottomRight = renderInverse.TransformPoint(new Point(Grid.ActualWidth, Grid.ActualHeight));

            //Check if the panning or zooming puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly
            bool outOfBounds = false;
            //Create a canvas space translation to correct the translation if necessary
            TranslateTransform fixTranslate = new TranslateTransform();
            if (topLeft.X < Bounds.Left && bottomRight.X > Bounds.Right)
            {
                translate.X = 0;
                fixTranslate.X = 0;
                double scaleAmount = (bottomRight.X - topLeft.X) / Bounds.Width;
                scale.ScaleY = scaleAmount;
                scale.ScaleX = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.X < Bounds.Left)
            {
                translate.X = 0;
                fixTranslate.X = preTopLeft.X;
                scale.CenterX = Bounds.Left;
                outOfBounds = true;
            }
            else if (bottomRight.X > Bounds.Right)
            {
                translate.X = 0;
                fixTranslate.X = -(Bounds.Right - preBottomRight.X - 1);
                scale.CenterX = Bounds.Right;
                outOfBounds = true;
            }
            if (topLeft.Y < Bounds.Top && bottomRight.Y > Bounds.Bottom)
            {
                translate.Y = 0;
                fixTranslate.Y = 0;
                double scaleAmount = (bottomRight.Y - topLeft.Y) / Bounds.Height;
                scale.ScaleX = scaleAmount;
                scale.ScaleY = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.Y < Bounds.Top)
            {
                translate.Y = 0;
                fixTranslate.Y = preTopLeft.Y;
                scale.CenterY = Bounds.Top;
                outOfBounds = true;
            }
            else if (bottomRight.Y > Bounds.Bottom)
            {
                translate.Y = 0;
                fixTranslate.Y = -(Bounds.Bottom - preBottomRight.Y - 1);
                scale.CenterY = Bounds.Bottom;
                outOfBounds = true;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                composite = new TransformGroup();
                composite.Children.Add(fixTranslate);
                composite.Children.Add(scale);
                composite.Children.Add(canvas.RenderTransform);
                composite.Children.Add(translate);
            }

            canvas.RenderTransform = new MatrixTransform { Matrix = composite.Value };
        }

        /// <summary>
        /// Zooms upon mousewheel interaction 
        /// </summary>
        private void UserControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            Canvas canvas = GridView.ItemsPanelRoot as Canvas;
            Debug.Assert(canvas != null);
            e.Handled = true;
            //Get mousepoint in canvas space 
            PointerPoint point = e.GetCurrentPoint(canvas);
            double scaleAmount = Math.Pow(1 + 0.15 * Math.Sign(point.Properties.MouseWheelDelta),
                Math.Abs(point.Properties.MouseWheelDelta) / 120.0f);
            scaleAmount = Math.Max(Math.Min(scaleAmount, 1.7f), 0.4f);
            CanvasScale *= (float)scaleAmount;
            Debug.Assert(canvas.RenderTransform != null);
            Point p = point.Position;
            Debug.WriteLine(p);
            //Create initial ScaleTransform 
            ScaleTransform scale = new ScaleTransform
            {
                CenterX = p.X,
                CenterY = p.Y,
                ScaleX = scaleAmount,
                ScaleY = scaleAmount
            };

            //Clamp scale
            if (CanvasScale > MaxScale)
            {
                CanvasScale = MaxScale;
                scale.ScaleX = MaxScale / CanvasScale;
                scale.ScaleY = MaxScale / CanvasScale;
            }
            if (CanvasScale < MinScale)
            {
                CanvasScale = MinScale;
                scale.ScaleX = MinScale / CanvasScale;
                scale.ScaleY = MinScale / CanvasScale;
            }

            //Create initial composite transform
            TransformGroup composite = new TransformGroup();
            composite.Children.Add(scale);
            composite.Children.Add(canvas.RenderTransform);

            GeneralTransform inverse = composite.Inverse;
            Debug.Assert(inverse != null);
            GeneralTransform renderInverse = canvas.RenderTransform.Inverse;
            Debug.Assert(inverse != null);
            Debug.Assert(renderInverse != null);
            Point topLeft = inverse.TransformPoint(new Point(0, 0));
            Point bottomRight = inverse.TransformPoint(new Point(Grid.ActualWidth, Grid.ActualHeight));
            Point preTopLeft = renderInverse.TransformPoint(new Point(0, 0));
            Point preBottomRight = renderInverse.TransformPoint(new Point(Grid.ActualWidth, Grid.ActualHeight));

            //Check if the zooming puts the view out of bounds of the canvas
            //Nullify scale or translate components accordingly 
            bool outOfBounds = false;
            //Create a canvas space translation to correct the translation if necessary
            TranslateTransform fixTranslate = new TranslateTransform();
            if (topLeft.X < Bounds.Left && bottomRight.X > Bounds.Right)
            {
                fixTranslate.X = 0;
                scaleAmount = (bottomRight.X - topLeft.X) / Bounds.Width;
                scale.ScaleY = scaleAmount;
                scale.ScaleX = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.X < Bounds.Left)
            {
                fixTranslate.X = preTopLeft.X;
                scale.CenterX = Bounds.Left;
                outOfBounds = true;
            }
            else if (bottomRight.X > Bounds.Right)
            {
                fixTranslate.X = -(Bounds.Right - preBottomRight.X - 1);
                scale.CenterX = Bounds.Right;
                outOfBounds = true;
            }
            if (topLeft.Y < Bounds.Top && bottomRight.Y > Bounds.Bottom)
            {
                fixTranslate.Y = 0;
                scaleAmount = (bottomRight.Y - topLeft.Y) / Bounds.Height;
                scale.ScaleX = scaleAmount;
                scale.ScaleY = scaleAmount;
                outOfBounds = true;
            }
            else if (topLeft.Y < Bounds.Top)
            {
                fixTranslate.Y = preTopLeft.Y;
                scale.CenterY = Bounds.Top;
                outOfBounds = true;
            }
            else if (bottomRight.Y > Bounds.Bottom)
            {
                fixTranslate.Y = -(Bounds.Bottom - preBottomRight.Y - 1);
                scale.CenterY = Bounds.Bottom;
                outOfBounds = true;
            }

            //If the view was out of bounds recalculate the composite matrix
            if (outOfBounds)
            {
                composite = new TransformGroup();
                composite.Children.Add(fixTranslate);
                composite.Children.Add(scale);
                composite.Children.Add(canvas.RenderTransform);
            }
            canvas.RenderTransform = new MatrixTransform { Matrix = composite.Value };
        }

        /// <summary>
        /// Make translation inertia slow down faster
        /// </summary>
        private void UserControl_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 0.01;
        }

        /// <summary>
        /// Make sure the canvas is still in bounds after resize
        /// </summary>
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TranslateTransform translate = new TranslateTransform();

            //Calculate bottomRight corner of screen in canvas space before and after resize 
            Debug.Assert(DocumentViewContainerGrid.RenderTransform != null);
            Debug.Assert(DocumentViewContainerGrid.RenderTransform.Inverse != null);
            Point oldBottomRight =
                DocumentViewContainerGrid.RenderTransform.Inverse.TransformPoint(new Point(e.PreviousSize.Width, e.PreviousSize.Height));
            Point bottomRight =
                DocumentViewContainerGrid.RenderTransform.Inverse.TransformPoint(new Point(e.NewSize.Width, e.NewSize.Height));

            //Check if new bottom right is out of bounds
            bool outOfBounds = false;
            if (bottomRight.X > Grid.ActualWidth - 1)
            {
                translate.X = -(oldBottomRight.X - bottomRight.X);
                outOfBounds = true;
            }
            if (bottomRight.Y > Grid.ActualHeight - 1)
            {
                translate.Y = -(oldBottomRight.Y - bottomRight.Y);
                outOfBounds = true;
            }
            //If it is out of bounds, translate so that is is in bounds
            if (outOfBounds)
            {
                TransformGroup composite = new TransformGroup();
                composite.Children.Add(translate);
                composite.Children.Add(DocumentViewContainerGrid.RenderTransform);
                DocumentViewContainerGrid.RenderTransform = new MatrixTransform { Matrix = composite.Value };
            }

            Clip = new RectangleGeometry { Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height) };
        }
    }
}
