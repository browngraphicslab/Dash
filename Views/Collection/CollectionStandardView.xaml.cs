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
            ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Detail;
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

        protected override void ManipulationControls_OnManipulatorTranslated(TransformGroupData transformation, bool abs)
        {
            base.ManipulationControls_OnManipulatorTranslated(transformation, abs);
            //var scaleX = transformation.ScaleAmount.X;
            //var scaleY = transformation.ScaleAmount.Y;
            //var currentView = (int)ViewModel.ViewLevel;
            //if (scaleX < 1 && scaleY < 1 && currentView > 1)
            //{
            //    var newView = currentView - 1;
            //    ViewModel.ViewLevel = (CollectionViewModel.StandardViewLevel) newView;
            //}
            //else if (scaleX > 1 && scaleY > 1 && currentView < 3)
            //{
            //    var newView = currentView + 1;
            //    ViewModel.ViewLevel = (CollectionViewModel.StandardViewLevel) newView;
            //}
            var scale = ViewManipulationControls.ElementScale;
            if (scale <= 0.3)
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

        private void UpdateViewLevel()
        {
            //var scale = ViewManipulationControls.ElementScale;
            var scale = ViewModel.PrevScale;
            if (scale <= 0.5)
            {
                ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Overview;
                MainPage.Instance.xMainTreeView.ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.Overview;
            }
            else if (scale <= 1.75)
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
