﻿using System;
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
using Windows.UI.Xaml.Media.Animation;

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
        private Storyboard _storyboard;
        private MatrixTransform _animatedTransform;

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
                zoom--;
                if (zoom < -3)
                {
                    if (currentView > 1 && ViewManipulationControls.ElementScale >= ViewManipulationControls.MinScale)
                    {
                        newView = currentView - 1;
                        newScale = SetScale((CollectionViewModel.StandardViewLevel)newView);
                    }
                    zoom = 0;
                }
            }
            else if (scaleX > 1 && scaleY > 1)
            {
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
            if (newScale != 1)
                AnimateZoom(matrix);
            else
                ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
            if (newView != currentView)
            {
                ViewModel.ViewLevel = (CollectionViewModel.StandardViewLevel)newView;
                MainPage.Instance.xMainTreeView.ViewModel.ViewLevel = (CollectionViewModel.StandardViewLevel)newView;
            }
        }

        /// <summary>
        /// Returns whether or not 
        /// </summary>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <param name="endScaleX"></param>
        /// <param name="endScaleY"></param>
        /// <returns></returns>
        private bool AnimateZoom(Matrix matrix)
        {
            var oldMatrix = (GetCanvas().RenderTransform as MatrixTransform)?.Matrix;
            if (oldMatrix == null) return false;
            _animatedTransform = new MatrixTransform() { Matrix = (Matrix)oldMatrix };
            var newMatrix = new MatrixTransform() { Matrix = matrix };

            var milliseconds = 300;
            var duration = new Duration(TimeSpan.FromMilliseconds(milliseconds));

            _storyboard?.SkipToFill();
            _storyboard?.Stop();
            _storyboard?.Children.Clear();
            _storyboard = new Storyboard() { Duration = duration };

            var startX = _animatedTransform.Matrix.OffsetX;
            var startY = _animatedTransform.Matrix.OffsetY;
            var endX = newMatrix.Matrix.OffsetX;
            var endY = newMatrix.Matrix.OffsetY;

            // Create a DoubleAnimation for translating
            var translateAnimationX = MakeAnimationElement(_animatedTransform, startX, endX, "MatrixTransform.Matrix.OffsetX", duration);
            var translateAnimationY = MakeAnimationElement(_animatedTransform, startY, endY, "MatrixTransform.Matrix.OffsetY", duration);
            translateAnimationX.AutoReverse = false;
            translateAnimationY.AutoReverse = false;

            var startScaleX = _animatedTransform.Matrix.M11;
            var startScaleY = _animatedTransform.Matrix.M22;
            var endScaleX = newMatrix.Matrix.M11;
            var endScaleY = newMatrix.Matrix.M22;

            var zoomAnimationX = MakeAnimationElement(_animatedTransform, startScaleX, Math.Max(0.2, endScaleX), "MatrixTransform.Matrix.M11", duration);
            var zoomAnimationY = MakeAnimationElement(_animatedTransform, startScaleY, Math.Max(0.2, endScaleY), "MatrixTransform.Matrix.M22", duration);

            zoomAnimationX.AutoReverse = false;
            zoomAnimationY.AutoReverse = false;

            _storyboard.Children.Add(translateAnimationX);
            _storyboard.Children.Add(translateAnimationY);
            _storyboard.Children.Add(zoomAnimationX);
            _storyboard.Children.Add(zoomAnimationY);

            CompositionTarget.Rendering -= CompositionTargetRendering;
            CompositionTarget.Rendering += CompositionTargetRendering;

            _storyboard.FillBehavior = FillBehavior.HoldEnd;
            _storyboard.Begin();
            _storyboard.Completed -= StoryboardCompleted;
            _storyboard.Completed += StoryboardCompleted;
            return true;
        }

        private void StoryboardCompleted(object sender, object e)
        {
            CompositionTarget.Rendering -= CompositionTargetRendering;
            _storyboard.Completed -= StoryboardCompleted;
        }

        void CompositionTargetRendering(object sender, object e)
        {
            var matrix = _animatedTransform.Matrix;
            ViewModel.TransformGroup = new TransformGroupData(new Point(matrix.OffsetX, matrix.OffsetY), new Point(matrix.M11, matrix.M22));
        }

        private double SetScale(CollectionViewModel.StandardViewLevel viewLevel)
        {
            double scale = 1;
            switch (viewLevel)
            {
                case CollectionViewModel.StandardViewLevel.None:
                    break;
                case CollectionViewModel.StandardViewLevel.Overview:
                    scale = ViewManipulationControls.MinScale / ViewManipulationControls.ElementScale;
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
