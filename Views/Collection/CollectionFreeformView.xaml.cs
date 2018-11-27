using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;

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

            _scaleX = 1.01;
            _scaleY = 1.01;
        }
        ~CollectionFreeformView()
        {
            //Debug.WriteLine("FINALIZING CollectionFreeFormView");
        }

        public void SetDisableTransformations()
        {
            ViewManipulationControls.SetDisableScrollWheel(true);
            ViewModel.DisableTransformations = true;
        }

        private void OnLoad(object sender, RoutedEventArgs e)
        {
        }
        public void OnDocumentSelected(bool selected)
        {
        }

        public override Panel GetTransformedCanvas()
        {
            return xTransformedCanvas;
        }

        public override DocumentView ParentDocument => this.GetFirstAncestorOfType<DocumentView>();
        public override ViewManipulationControls ViewManipulationControls { get; set; }

        public override CollectionViewModel ViewModel => DataContext as CollectionViewModel;

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
                e.Handled = true;
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
                e.Handled = true;
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
                e.Handled = true;
            }
        }
        public void AddToMenu(ActionMenu menu)
        {
            ImageSource source = new BitmapImage(new Uri("ms-appx://Dash/Assets/Rightlg.png"));
            menu.AddAction("BASIC", new ActionViewModel("Text",                "Add a new text box!", AddTextNote, source));
            menu.AddAction("BASIC", new ActionViewModel("Add Captioned Image", "Add an image with a caption below", AddImageWithCaption, source));
            menu.AddAction("BASIC", new ActionViewModel("Add Discussion",      "Add a discussion", AddDiscussion, source));
            menu.AddAction("BASIC", new ActionViewModel("Add Image(s)",        "Add one or more images",  AddMultipleImages, source));
            menu.AddAction("BASIC", new ActionViewModel("Add Collection",      "Collection",AddCollection,source));

            var templates = MainPage.Instance.MainDocument.GetDataDocument()
                .GetFieldOrCreateDefault<ListController<DocumentController>>(KeyStore.TemplateListKey);
            foreach (var template in templates)
            {
                var avm = new ActionViewModel(template.GetTitleFieldOrSetDefault().Data,
                    template.GetField<TextController>(KeyStore.CaptionKey).Data, actionParams  =>
                    {
                        var colPoint = MainPage.Instance.xCanvas.TransformToVisual(GetTransformedCanvas()).TransformPoint(actionParams.Where);
                        Actions.DisplayDocument(ViewModel, template.GetCopy(), colPoint);
                        return Task.FromResult(true);
                    }, source);
                menu.AddAction("CUSTOM", avm);
            }
        }

        

        private Task<bool> AddTextNote(ActionFuncParams actionParams)
        {
            var postitNote = new RichTextNote().Document;
            var colPoint = MainPage.Instance.xCanvas.TransformToVisual(GetTransformedCanvas()).TransformPoint(actionParams.Where);
            Actions.DisplayDocument(ViewModel, postitNote, colPoint);
            return Task.FromResult(true);
        }

        private Task<bool> AddCollection(ActionFuncParams actionParams)
        {
            var colPoint = MainPage.Instance.xCanvas.TransformToVisual(GetTransformedCanvas()).TransformPoint(actionParams.Where);
            var cnote = new CollectionNote(new Point(), CollectionViewType.Icon, 200, 75).Document;
            Actions.DisplayDocument(ViewModel, cnote, colPoint);
            return Task.FromResult(true);
        }

        private async Task<bool> AddMultipleImages(ActionFuncParams actionParams)
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

                var colPoint = MainPage.Instance.xCanvas.TransformToVisual(GetTransformedCanvas()).TransformPoint(actionParams.Where);
                var adornFormPoint = colPoint;
                var adorn = Util.AdornmentWithPosandColor(Colors.White, BackgroundShape.AdornmentShape.RoundedRectangle, adornFormPoint, (defaultLength * imagesToAdd.Count) + 20 + (5 * (imagesToAdd.Count - 1)), defaultLength + 40);
                ViewModel.AddDocument(adorn);

                int counter = 0;
                foreach (var thisImage in imagesToAdd)
                {
                    var parser = new ImageToDashUtil();
                    var docController = await parser.ParseFileAsync(thisImage);
                    if (docController == null) { continue; }
                    var pos = new Point(10 + colPoint.X + (counter * (defaultLength + 5)), colPoint.Y+10);
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
        private async Task<bool> AddDiscussion(ActionFuncParams actionParams)
        {
            var pt = MainPage.Instance.xCanvas.TransformToVisual(GetTransformedCanvas()).TransformPoint(actionParams.Where);
            var docController = new DiscussionNote("testing...", pt).Document;
            var note1 = new RichTextNote("Testing...").Document;
            note1.GetDataDocument().SetField<NumberController>(KeyController.Get("DiscussionDepth"), 0, true);
            docController.GetDataDocument().SetField(KeyController.Get("DiscussionItems"), new ListController<DocumentController>(note1), true);
            docController.GetDataDocument().SetField<NumberController>(KeyController.Get("DiscussionDepth"), 1, true);
            docController.SetWidth(double.NaN);
            docController.SetHeight(double.NaN);
            docController.SetHorizontalAlignment(HorizontalAlignment.Left);
            docController.SetVerticalAlignment(VerticalAlignment.Stretch);
            ViewModel.AddDocument(docController);

            return true;
        }
        private async Task<bool> AddImageWithCaption(ActionFuncParams actionParams)
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
                    docController.SetField<TextController>(KeyStore.XamlKey,
                        @"<Grid  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                                 xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
                                 xmlns:dash=""using:Dash""
                                 xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006"" >
                            <Grid.RowDefinitions>
                                <RowDefinition Height=""Auto"" ></RowDefinition>
                                <RowDefinition Height=""*"" ></RowDefinition>
                            </Grid.RowDefinitions>
                                <Border Grid.Row=""0"" Background =""CadetBlue"" >
                                    <dash:EditableImage x:Name=""xImageFieldData"" Foreground =""White"" HorizontalAlignment =""Stretch"" Grid.Row=""1"" VerticalAlignment =""Top"" />
                                </Border>
                                <Border Grid.Row=""1"" Background =""CadetBlue"" MinHeight =""30"" >
                                    <dash:RichEditView x:Name= ""xRichTextFieldCaption"" TextWrapping= ""Wrap"" Foreground= ""White"" HorizontalAlignment= ""Stretch"" Grid.Row= ""1"" VerticalAlignment= ""Top"" />
                                </Border>
                        </Grid>",
                        true);
                    var imagePt = MainPage.Instance.xCanvas.TransformToVisual(GetTransformedCanvas()).TransformPoint(actionParams.Where);
                    docController.SetWidth(docController.GetWidth());
                    docController.SetHeight(double.NaN);
                    docController.SetHorizontalAlignment(HorizontalAlignment.Stretch);
                    docController.SetVerticalAlignment(VerticalAlignment.Top);
                    docController.SetPosition(new Point(imagePt.X, imagePt.Y));
                    ViewModel.AddDocument(docController);
                }
            }

            return true;
        }
    }
}
