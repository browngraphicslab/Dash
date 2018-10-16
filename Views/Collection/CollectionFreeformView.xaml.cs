using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Pickers;
using Windows.UI;
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
            xOuterGrid.PointerCanceled += OnPointerCancelled;
            //xOuterGrid.PointerCaptureLost += OnPointerReleased;

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
            menu.AddAction("BASIC", new ActionViewModel("Add Captioned Image", "Add an image with a caption below", AddImageWithCaption, source));
            menu.AddAction("BASIC", new ActionViewModel("Add Image", "Add many images",  AddMultipleImages, source));
            menu.AddAction("BASIC", new ActionViewModel("Add Collection", "Collection",AddCollection,source));

            var templates = MainPage.Instance.MainDocument.GetDataDocument()
                .GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.TemplateListKey).TypedData;
            foreach (var template in templates)
            {
                var avm = new ActionViewModel(template.GetTitleFieldOrSetDefault().Data,
                    template.GetField<TextController>(KeyStore.CaptionKey).Data, point =>
                    {
                        var colPoint = MainPage.Instance.xCanvas.TransformToVisual(GetCanvas()).TransformPoint(point);
                        Actions.DisplayDocument(ViewModel, template.GetCopy(), colPoint);
                        return Task.FromResult(true);
                    }, source);
                menu.AddAction("CUSTOM", avm);
            }
        }

        private Task<bool> AddToDoList(Point point)
        {
            var templatedText =
                "{\\rtf1\\fbidis\\ansi\\ansicpg1252\\deff0\\nouicompat\\deflang1033{\\fonttbl{\\f0\\fnil Century Gothic; } {\\f1\\fnil\\fcharset0 Century Gothic; } {\\f2\\fnil\\fcharset2 Symbol; } }" +
                "\r\n{\\colortbl;\\red51\\green51\\blue51; }\r\n{\\*\\generator Riched20 10.0.17134}\\viewkind4\\uc1 \r\n\\pard\\tx720\\cf1\\b{\\ul\\f0\\fs34 My\\~\\f1 Todo\\~List:}\\par" +
                "\r\n\\b0\\f0\\fs24\\par\r\n\r\n\\pard{\\pntext\\f2\\'B7\\tab}{\\*\\pn\\pnlvlblt\\pnf2\\pnindent0{\\pntxtb\\'B7}}\\tx720\\f1\\fs24 Item\\~1\\par" +
                "\r\n{\\pntext\\f2\\'B7\\tab}\\b0 Item\\~2\\par}";
            var note = new RichTextNote(templatedText).Document;
            var colPoint = MainPage.Instance.xCanvas.TransformToVisual(GetCanvas()).TransformPoint(point);
            Actions.DisplayDocument(ViewModel, note, colPoint);
            return Task.FromResult(true);
        }

        

        private Task<bool> AddTextNote(Point point)
        {
            var postitNote = new RichTextNote().Document;
            var colPoint = MainPage.Instance.xCanvas.TransformToVisual(GetCanvas()).TransformPoint(point);
            Actions.DisplayDocument(ViewModel, postitNote, colPoint);
            return Task.FromResult(true);
        }

        private Task<bool> AddCollection(Point point)
        {
            var colPoint = MainPage.Instance.xCanvas.TransformToVisual(GetCanvas()).TransformPoint(point);
            var cnote = new CollectionNote(new Point(), CollectionView.CollectionViewType.Icon, 200, 75).Document;
            Actions.DisplayDocument(ViewModel, cnote, colPoint);
            return Task.FromResult(true);
        }

        private async Task<bool> AddMultipleImages(Point point)
        {
            var imagePicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            imagePicker.FileTypeFilter.Add(".jpg");
            imagePicker.FileTypeFilter.Add(".jpeg");
            imagePicker.FileTypeFilter.Add(".bmp");
            imagePicker.FileTypeFilter.Add(".png");
            imagePicker.FileTypeFilter.Add(".svg");

            //adds each image selected to Dash
            var imagesToAdd = await imagePicker.PickMultipleFilesAsync();

            //TODO just add new images to docs list instead of going through mainPageCollectionView
            //var docs = MainPage.Instance.MainDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            if (imagesToAdd != null)
            {
                double defaultLength = 200;

                var colPoint = MainPage.Instance.xCanvas.TransformToVisual(GetCanvas()).TransformPoint(point);
                var adornFormPoint = colPoint;
                var adorn = Util.AdornmentWithPosandColor(Colors.LightGray, BackgroundShape.AdornmentShape.RoundedRectangle, adornFormPoint, (defaultLength * imagesToAdd.Count) + 20 + (5 * (imagesToAdd.Count - 1)), defaultLength + 40);
                ViewModel.AddDocument(adorn);

                int counter = 0;
                foreach (var thisImage in imagesToAdd)
                {
                    var parser = new ImageToDashUtil();
                    var docController = await parser.ParseFileAsync(thisImage);
                    if (docController == null) { continue; }
                    var pos = new Point(colPoint.X + (counter * (defaultLength + 5)), colPoint.Y);
                    docController.SetWidth(defaultLength);
                    docController.SetHeight(defaultLength);
                    Actions.DisplayDocument(ViewModel, docController, pos);
                    counter++;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        private async Task<bool> AddImageWithCaption(Point point)
        {
            var imagePicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            imagePicker.FileTypeFilter.Add(".jpg");
            imagePicker.FileTypeFilter.Add(".jpeg");
            imagePicker.FileTypeFilter.Add(".bmp");
            imagePicker.FileTypeFilter.Add(".png");
            imagePicker.FileTypeFilter.Add(".svg");

            //adds each image selected to Dash
            var imageToAdd = await imagePicker.PickSingleFileAsync();

            //TODO just add new images to docs list instead of going through mainPageCollectionView
            //var docs = MainPage.Instance.MainDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            if (imageToAdd != null)
            {
                var parser = new ImageToDashUtil();
                var docController = await parser.ParseFileAsync(imageToAdd);
                if (docController != null)
                {
                    double imageWidth = docController.GetWidth();
                    double imageHeight = docController.GetHeight();
                    docController.SetWidth(double.NaN);
                    docController.SetHeight(double.NaN);
                    var imagePt = MainPage.Instance.xCanvas.TransformToVisual(GetCanvas()).TransformPoint(point);
                    var caption = new RichTextNote(docController.Title).Document;
                    caption.SetHorizontalAlignment(HorizontalAlignment.Center);
                    docController.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                    var adorn = new CollectionNote(new Point(imagePt.X, imagePt.Y), CollectionView.CollectionViewType.Stacking, 300, imageHeight / imageWidth * 300 + 30, new DocumentController[] { docController, caption });
                    ViewModel.AddDocument(adorn.Document);
                }
            }

            return true;
        }
    }
}
