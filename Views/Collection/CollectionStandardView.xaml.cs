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
using Windows.UI.Xaml.Shapes;
using Microsoft.Graphics.Canvas.UI.Xaml;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionStandardView
    {

        public override DocumentView ParentDocument => this.GetFirstAncestorOfType<DocumentView>();
        public override ViewManipulationControls ViewManipulationControls { get; set; }

        public override CollectionViewModel ViewModel => DataContext as CollectionViewModel;
        public override CollectionView.CollectionViewType Type
        {
            get
            {
                return CollectionView.CollectionViewType.Standard;
            }
        }

        public CollectionStandardView(): base()
        {
            this.InitializeComponent();
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoad;
            Unloaded += OnUnload;
            xOuterGrid.PointerEntered += OnPointerEntered;
            xOuterGrid.PointerExited += OnPointerExited;
            xOuterGrid.SizeChanged += OnSizeChanged;
            xOuterGrid.PointerPressed += OnPointerPressed;
            xOuterGrid.PointerReleased += OnPointerReleased;
            ViewManipulationControls = new ViewManipulationControls(this);
            ViewManipulationControls.OnManipulatorTranslatedOrScaled += ManipulationControls_OnManipulatorTranslated;
        }

        protected override void OnLoad(object sender, RoutedEventArgs e)
        {
            base.OnLoad(sender,e);
            if (ViewModel.PrevScale != 0)
                ViewManipulationControls.ElementScale = ViewModel.PrevScale;
            ViewManipulationControls.IsScaleDiscrete = true;
            UpdateViewLevel();
        }

        public override Canvas GetCanvas()
        {
            return xItemsControl.ItemsPanelRoot as Canvas;
        }

        public override ItemsControl GetItemsControl()
        {
            return xItemsControl;
        }

        public override CanvasControl GetBackgroundCanvas()
        {
            return xBackgroundCanvas;
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

        private int zoom = 0;
        protected override void ManipulationControls_OnManipulatorTranslated(TransformGroupData transformation,
            bool abs)
        {
            var scaleX = transformation.ScaleAmount.X;
            var scaleY = transformation.ScaleAmount.Y;
            var currentView = (int)ViewModel.ViewLevel;
            var newView = currentView;
            double newScale = 1;
            if (scaleX < 1 && scaleY < 1)
            {
                //zoomOut++;
                zoom--;
                if (zoom < -3)
                {
                    if (currentView > 1 && ViewManipulationControls.ElementScale >= ViewManipulationControls.MinScale + 0.01)
                    {
                        newView = currentView - 1;
                        newScale = SetScale((CollectionViewModel.StandardViewLevel)newView);
                    }
                    zoom = 0;
                }
            }
            else if (scaleX > 1 && scaleY > 1)
            {
                //zoomIn++;
                zoom++;
                if (zoom > 3)
                {
                    if (currentView < 3 && ViewManipulationControls.ElementScale <= 2)
                    {
                        newView = currentView + 1;
                        newScale = SetScale((CollectionViewModel.StandardViewLevel)newView);
                    }
                    zoom = 0;
                }
            }


            //if (zoomOut >= 3)
            //{
            //    zoomOut = 0;
            //    zoomIn = 0;
            //    if (currentView > 1 && ViewManipulationControls.ElementScale >= ViewManipulationControls.MinScale + 0.01)
            //    {
            //        newView = currentView - 1;
            //        newScale = SetScale((CollectionViewModel.StandardViewLevel)newView);
            //    }
            //}
            //if (zoomIn >= 3)
            //{
            //    zoomOut = 0;
            //    zoomIn = 0;
            //    if (currentView < 3 && ViewManipulationControls.ElementScale < ViewManipulationControls.MaxScale)
            //    {
            //        newView = currentView + 1;
            //        newScale = SetScale((CollectionViewModel.StandardViewLevel) newView);
            //    }
            //    else
            //    {
            //        newScale = scaleX;
            //    }
            //}

            // calculate the translate delta
            var translateDelta = new TranslateTransform
            {
                X = transformation.Translate.X,
                Y = transformation.Translate.Y
            };

            // calculate the scale delta
            var scaleDelta = new ScaleTransform
            {
                CenterX = transformation.ScaleCenter.X,
                CenterY = transformation.ScaleCenter.Y,
                ScaleX = newScale,
                ScaleY = newScale
            };

            //Create initial composite transform
            var composite = new TransformGroup();
            if (!abs)
                composite.Children.Add(GetCanvas().RenderTransform); // get the current transform
            composite.Children.Add(scaleDelta); // add the new scaling
            composite.Children.Add(translateDelta); // add the new translate

            var matrix = composite.Value;
            ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));

            if (newView != currentView)
            {
                ViewModel.ViewLevel = (CollectionViewModel.StandardViewLevel)newView;
                MainPage.Instance.xMainTreeView.ViewModel.ViewLevel = (CollectionViewModel.StandardViewLevel)newView;
            }
            //var scale = ViewManipulationControls.ElementScale;
            //if (scale <= 0.3)
            //{
            //    ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Overview;
            //    MainPage.Instance.xMainTreeView.ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Overview;
            //}
            //else if (scale <= 1)
            //{
            //    ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Region;
            //    MainPage.Instance.xMainTreeView.ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Region;
            //}
            //else
            //{
            //    ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Detail;
            //    MainPage.Instance.xMainTreeView.ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Detail;
            //}
        }

        private double SetScale(CollectionViewModel.StandardViewLevel viewLevel)
        {
            double scale = 1;
            switch (viewLevel)
            {
                case CollectionViewModel.StandardViewLevel.None:
                    break;
                case CollectionViewModel.StandardViewLevel.Overview:
                    scale = ViewManipulationControls.MinScale / ViewManipulationControls.ElementScale + 0.01;
                    ViewManipulationControls.ElementScale = ViewManipulationControls.ElementScale * scale;
                    break;
                case CollectionViewModel.StandardViewLevel.Region:
                    scale =  1 / ViewManipulationControls.ElementScale;
                    ViewManipulationControls.ElementScale = ViewManipulationControls.ElementScale * scale;
                    break;
                case CollectionViewModel.StandardViewLevel.Detail:
                    scale = 2 / ViewManipulationControls.ElementScale;
                    ViewManipulationControls.ElementScale = ViewManipulationControls.ElementScale * scale;
                    break;
            }
            return scale;
        }

        private void UpdateViewLevel()
        {
            var scale = ViewModel.PrevScale;
            if (scale <= 0.5)
            {
                ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Overview;
                MainPage.Instance.xMainTreeView.ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Overview;
            }
            else if (scale <= 1)
            {
                ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Region;
                MainPage.Instance.xMainTreeView.ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Region;
            }
            else
            {
                ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Detail;
                MainPage.Instance.xMainTreeView.ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Detail;
            }
        }
    }
}
