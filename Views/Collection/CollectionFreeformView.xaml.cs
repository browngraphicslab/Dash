using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Visibility = Windows.UI.Xaml.Visibility;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Animation;
using Dash.Annotations;
using Dash.Views.Collection;
using DashShared;
using NewControls.Geometry;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class CollectionFreeformView
    {
        public CollectionFreeformView(): base()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoad;
            Unloaded += OnUnload;
            xOuterGrid.PointerEntered += OnPointerEntered;
            xOuterGrid.PointerExited += OnPointerExited;
            xOuterGrid.SizeChanged += OnSizeChanged;
            xOuterGrid.PointerPressed  += OnPointerPressed;
            xOuterGrid.PointerReleased += OnPointerReleased;
            ViewManipulationControls = new ViewManipulationControls(this);
            ViewManipulationControls.OnManipulatorTranslatedOrScaled += ManipulationControls_OnManipulatorTranslated;
        }

        protected override void OnLoad(object sender, RoutedEventArgs e)
        {
            base.OnLoad(sender, e);
            if (ViewModel.PrevScale != 0)
                ViewManipulationControls.ElementScale = ViewModel.PrevScale;
            ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.None;
            MainPage.Instance.xMainTreeView.ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.None;
            if (this.GetFirstAncestorOfType<DocumentView>() != null)
            {
                this.GetFirstAncestorOfType<DocumentView>().ViewModel.ViewLevel = CollectionViewModel.StandardViewLevel.None;
            }
            SelectionManager.RefreshSelected(this.GetDescendantsOfType<DocumentView>());
        }

        public override Canvas GetCanvas()
        {
            return xItemsControl.ItemsPanelRoot as Canvas;
        }

        public override DocumentView ParentDocument => this.GetFirstAncestorOfType<DocumentView>();
        public override ViewManipulationControls ViewManipulationControls { get; set; }

        public override CollectionViewModel ViewModel => DataContext as CollectionViewModel;

        public override CollectionView.CollectionViewType Type => CollectionView.CollectionViewType.Freeform;

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
    }
}