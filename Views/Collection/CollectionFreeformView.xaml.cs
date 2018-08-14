using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Shapes;

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
            xOuterGrid.PointerPressed += OnPointerPressed;
            xOuterGrid.PointerReleased += OnPointerReleased;
            ViewManipulationControls = new ViewManipulationControls(this);
            ViewManipulationControls.OnManipulatorTranslatedOrScaled += ManipulationControls_OnManipulatorTranslated;
        }
        ~CollectionFreeformView()
        {
            Debug.WriteLine("FINALIZING CollectionFreeFormView");
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