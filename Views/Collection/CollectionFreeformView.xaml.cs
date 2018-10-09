using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using DashShared;

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

        CoreCursor Arrow = new CoreCursor(CoreCursorType.Arrow, 1);
        private void xOuterGrid_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {

            if (!this.IsLeftBtnPressed() && !this.IsRightBtnPressed())
            {
                Window.Current.CoreWindow.PointerCursor = Arrow;

                e.Handled = true;
            }
        }

        public void AddToMenu(ActionMenu menu)
        {
            ImageSource source = new BitmapImage(new Uri("ms-appx://Dash/Assets/Rightlg.png"));
            menu.AddAction("BASIC", new ActionViewModel("Text", "Add a new text box!", AddTextNote, source));
            menu.AddAction("BASIC", new ActionViewModel("To-Do List", "Track your tasks!", AddToDoList, source));
        }

        private bool AddToDoList(Point point)
        {
            var templatedText =
                "{\\rtf1\\fbidis\\ansi\\ansicpg1252\\deff0\\nouicompat\\deflang1033{\\fonttbl{\\f0\\fnil Century Gothic; } {\\f1\\fnil\\fcharset0 Century Gothic; } {\\f2\\fnil\\fcharset2 Symbol; } }" +
                "\r\n{\\colortbl;\\red51\\green51\\blue51; }\r\n{\\*\\generator Riched20 10.0.17134}\\viewkind4\\uc1 \r\n\\pard\\tx720\\cf1\\b{\\ul\\f0\\fs34 My\\~\\f1 Todo\\~List:}\\par" +
                "\r\n\\b0\\f0\\fs24\\par\r\n\r\n\\pard{\\pntext\\f2\\'B7\\tab}{\\*\\pn\\pnlvlblt\\pnf2\\pnindent0{\\pntxtb\\'B7}}\\tx720\\f1\\fs24 Item\\~1\\par" +
                "\r\n{\\pntext\\f2\\'B7\\tab}\\b0 Item\\~2\\par}";
            var note = new RichTextNote(templatedText).Document;
            var colPoint = MainPage.Instance.xCanvas.TransformToVisual(GetCanvas()).TransformPoint(point);
            Actions.DisplayDocument(ViewModel, note, colPoint);
            return true;
        }

        private bool AddTextNote(Point point)
        {
            var postitNote = new RichTextNote().Document;
            var colPoint = MainPage.Instance.xCanvas.TransformToVisual(GetCanvas()).TransformPoint(point);
            Actions.DisplayDocument(ViewModel, postitNote, colPoint);
            return true;
        }
    }
}
