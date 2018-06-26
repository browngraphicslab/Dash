using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Dash.Annotations;
using Dash.Converters;
using Microsoft.Office.Interop.Word;
using Point = Windows.Foundation.Point;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Dash
{
    public sealed partial class TemplateEditorView : UserControl
    {
        public DocumentController LayoutDocument { get; set; }
        public DocumentController DataDocument { get; set; }

        public ObservableCollection<DocumentController> DocumentControllers { get; set; }

        public ObservableCollection<DocumentViewModel> DocumentViewModels { get; set; }

        public Collection<DocumentView> DocumentViews { get; set; }

        private KeyValueTemplatePane _keyValuePane;
        private DocumentView _selectedDocument;

		public TemplateEditorView()
	    {
		    this.InitializeComponent();
	        //this.GetFirstAncestorOfType<DocumentView>().ViewModel.DecorationState = false;
	        //this.GetFirstAncestorOfType<DocumentView>().hideControls();

            DocumentControllers = new ObservableCollection<DocumentController>();
	        DocumentViewModels = new ObservableCollection<DocumentViewModel>();
	        DocumentViews = new Collection<DocumentView>();
	    }

        private void DocumentControllers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddDocs(e.NewItems.Cast<DocumentController>());
                    break;
                case NotifyCollectionChangedAction.Move:
                    break;
                case NotifyCollectionChangedAction.Remove:
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        private void AddDocs(IEnumerable<DocumentController> newDocs)
        {
            foreach (var doc in newDocs)
            {
                // create new viewmodel with a copy of document, set editor to this
                var dvm =
                    new DocumentViewModel(doc.GetViewCopy(new Point(0, 0)), new Context(doc));
                DocumentViewModels.Add(dvm);
                // check that the layout doc doesn't already exist in data document's list of layout docs
                // if it already exists, don't add it again to the data document
                DataDocument.GetField<ListController<DocumentController>>(KeyStore.DataKey).Add(dvm.LayoutDocument);
            }

            xItemsControl.ItemsSource = DocumentViewModels;

            //var dvms = xItemsControl.Items.Cast<DocumentViewModel>();
            //foreach (var dvm in dvms)
            //{
            //    // idk what to do from here, we have the viewmodel but we need the view to change manipulations
            //}
        }

        public void Load()
        {
	        this.UpdatePanes();
            var rect = new Rect(0, 0, 300, 400);
            var rectGeo = new RectangleGeometry {Rect = rect};
            xWorkspace.Clip = rectGeo;
        }

	    public void UpdatePanes()
	    {
			//make key value pane
		    if (xDataPanel.Children.Count == 0)
		    {
				_keyValuePane = new KeyValueTemplatePane(this);
			    xDataPanel.Children.Add(_keyValuePane);
			}

            //make central collection/canvas
	        DocumentControllers =
	            new ObservableCollection<DocumentController>(DataDocument
	                .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData);
           
        }

        private void XWorkspace_OnLoaded(object sender, RoutedEventArgs e)
        {
            DocumentViewModels.Clear();
            foreach (var layoutDoc in DataDocument
                .GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null).TypedData)
            {
                DocumentViewModels.Add(new DocumentViewModel(layoutDoc));
            }
            xItemsControl.ItemsSource = DocumentViewModels;
            DocumentControllers.CollectionChanged += DocumentControllers_CollectionChanged;
        }

        private void XWorkspace_OnUnloaded(object sender, RoutedEventArgs e)
        {
            DocumentControllers.CollectionChanged -= DocumentControllers_CollectionChanged;
        }

        private void TextButton_OnClick(object sender, RoutedEventArgs e)
        {
            DocumentControllers.Add(new RichTextNote("New text box").Document);
        }

        private async void ImageButton_OnClick(object sender, RoutedEventArgs e)
        {
            //opens file picker and limits search by listed image extensions
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
            
            if (imagesToAdd != null)
            {
                foreach (var thisImage in imagesToAdd)
                {
                    var parser = new ImageToDashUtil();
                    var docController = await parser.ParseFileAsync(thisImage);
                    if (docController != null)
                    {
                        if (docController.GetWidthField().Data >= xWorkspace.Width)
                            docController.SetWidth(xWorkspace.Width - 20);
                        // TODO: Check for if height is too large (may be difficult bcs height = nan? -sy
                        DocumentControllers.Add(docController);
                    }
                }
            }
        }

        private async void VideoButton_OnClick(object sender, RoutedEventArgs e)
        {
            //instantiates a file picker, set to open in user's video library
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.VideosLibrary
            };

            picker.FileTypeFilter.Add(".avi");
            picker.FileTypeFilter.Add(".mp4");
            picker.FileTypeFilter.Add(".wmv");

            //awaits user upload of video 
            var files = await picker.PickMultipleFilesAsync();

            //TODO just add new images to docs list instead of going through mainPageCollectionView
            //var docs = MainPage.Instance.MainDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            if (files != null)
            {
                foreach (var file in files)
                {
                    //create a doc controller for the video, set position, and add to canvas
                    var docController = await new VideoToDashUtil().ParseFileAsync(file);
                    if (docController != null)
                    {
                        if (docController.GetWidthField().Data >= xWorkspace.Width)
                            docController.SetWidth(xWorkspace.Width - 20);
                        // TODO: Check for if height is too large (may be difficult bcs height = nan? -sy
                        DocumentControllers.Add(docController);
                    }
                }

                //add error message for null file?
            }
        }

        private async void AudioButton_OnClick(object sender, RoutedEventArgs e)
        {
            //instantiates a file picker, set to open in user's audio library
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.MusicLibrary
            };

            picker.FileTypeFilter.Add(".mp3");


            //awaits user upload of audio 
            var files = await picker.PickMultipleFilesAsync();

            //TODO just add new images to docs list instead of going through mainPageCollectionView
            //var docs = MainPage.Instance.MainDocument.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.DataKey, null);
            if (files != null)
            {
                foreach (var file in files)
                {
                    //create a doc controller for the audio, set position, and add to canvas
                    var docController = await new AudioToDashUtil().ParseFileAsync(file);
                    if (docController != null)
                    {
                        if (docController.GetWidthField().Data >= xWorkspace.Width)
                            docController.SetWidth(xWorkspace.Width - 20);
                        DocumentControllers.Add(docController);
                    }
                }
                //add error message for null file?
            }
        }

        private void AlignmentButton_OnChecked(object sender, RoutedEventArgs e)
        {
            var button = sender as AppBarButton;

            //for each document, align according to what button was pressed
            foreach (var dvm in DocumentViewModels)
            {
                var point = dvm.LayoutDocument.GetField<PointController>(KeyStore.PositionFieldKey);
                switch (button.Name)
                {
                    case "xAlignLeftButton":
                        point = new PointController(0, point.Data.Y);
                        break;

                    case "xAlignCenterButton":
                        var centerX = (xWorkspace.Width - dvm.LayoutDocument.GetActualSize().Value.X) / 2;
                        point = new PointController(centerX, point.Data.Y);
                        break;

                    case "xAlignRightButton":
                        var rightX = xWorkspace.Width - dvm.LayoutDocument.GetActualSize().Value.X;
                        point = new PointController(rightX, point.Data.Y);
                        break;
                }
                dvm.LayoutDocument.SetField(KeyStore.PositionFieldKey, point, true);
            }
        }

        private void LeftBorder_OnChecked(object sender, RoutedEventArgs e)
        {
            
        }

        private void TopBorder_OnChecked(object sender, RoutedEventArgs e)
        {

        }

        private void RightBorder_OnChecked(object sender, RoutedEventArgs e)
        {

        }

        private void BottomBorder_OnChecked(object sender, RoutedEventArgs e)
        {

        }

        private void LeftBorder_OnUnchecked(object sender, RoutedEventArgs e)
        {

        }

        private void TopBorder_OnUnchecked(object sender, RoutedEventArgs e)
        {

        }

        private void RightBorder_OnUnchecked(object sender, RoutedEventArgs e)
        {

        }

        private void BottomBorder_OnUnchecked(object sender, RoutedEventArgs e)
        {

        }

        private void ApplyChanges_OnClicked(object sender, RoutedEventArgs e)
        {
            foreach (var doc in DocumentControllers)
            {
                if (doc.GetDataDocument().Equals(LayoutDocument.GetField<DocumentController>(KeyStore.DataKey)))
                {
                    // apply layout doc with abstraction
                }
                else
                {
                    // apply layout doc statically
                    
                }
            }
        }

        private void DocumentView_OnLoaded(object sender, RoutedEventArgs e)
        {
            var docView = sender as DocumentView;
            if (!DocumentViews.Contains(docView))
            {
                DocumentViews.Add(docView);
            }
            
            var bounds = new Rect(0, 0, xWorkspace.Clip.Rect.Width - docView.ActualWidth,
                xWorkspace.Clip.Rect.Height - docView.ActualHeight);
            docView.Bounds = new RectangleGeometry { Rect = bounds };
            docView.DocumentSelected += DocView_DocumentSelected;
        }

        private void DocView_DocumentSelected(DocumentView sender, DocumentView.DocumentViewSelectedEventArgs args)
        {
            _selectedDocument = sender;
        }

        private void TemplateEditorView_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var focused = this.Focus(FocusState.Programmatic);
        }
    }
}
