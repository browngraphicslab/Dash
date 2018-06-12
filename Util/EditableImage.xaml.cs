using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Dash.Annotations;
using DashShared;
using Microsoft.Toolkit.Uwp.UI.Extensions;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Dash
{
    public partial class EditableImage : INotifyPropertyChanged
    {
        private readonly Context _context;
        private readonly DocumentController _docCtrl;
        private StateCropControl _cropControl;
        private ImageController _imgctrl;
        private ImageSource _imgSource;
        private bool _isCropping;
        private double _originalWidth;
        private DocumentView _docview;
        private Image _originalImage;
        private Point _anchorPoint;
        private bool _isDragging;
        private ImageSource _ogImage;

        public Rect RectGeo;
        private double _ogWidth;
        private Uri _ogUri;

        public Image Image => xImage;

        public ImageSource ImageSource
        {
            get => _imgSource;
            set
            {
                _imgSource = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public EditableImage(DocumentController docCtrl, Context context)
        {
            InitializeComponent();
            _docCtrl = docCtrl;
            _context = context;
            Image.Loaded += Image_Loaded;
            Image.SizeChanged += Image_SizeChanged;
            // gets datakey value (which holds an imagecontroller) and cast it as imagecontroller
            _imgctrl = docCtrl.GetDereferencedField(KeyStore.DataKey, context) as ImageController;
        }

        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Focus(FocusState.Keyboard);
            _cropControl = new StateCropControl(_docCtrl, this);
        }

        private async void OnReplaceImage()
        {
            _imgctrl = _docCtrl.GetDereferencedField<ImageController>(KeyStore.DataKey, new Context());

            // finds local uri path of image controller's image source
            StorageFile file;

            /*
             * try catch is literally the only way we can deal with regular
             * local uris, absolute uris, and website uris as the same time
             */
            try
            {
                // method of getting file from local uri
                file = await StorageFile.GetFileFromPathAsync(_imgctrl.ImageSource.LocalPath);
            }
            catch (Exception)
            {
                // method of getting file from absolute uri
                file = await StorageFile.GetFileFromApplicationUriAsync(_imgctrl.ImageSource);
            }

            var fileProperties = await file.Properties.GetImagePropertiesAsync();
            _originalWidth = fileProperties.Width;
            //var newImg = new Image();
            //newImg.Source = new BitmapImage(_docCtrl.GetField<ImageController>(KeyStore.DataKey).Data);
            Image.Width = double.NaN;
            Image.Source = new BitmapImage(new Uri(file.Path));

            _ogImage = Image.Source;
            _ogWidth = _originalWidth;
            _ogUri = _imgctrl.ImageSource;
            /*
             *  onReplaceClicked
             *      _ogImage = new image.source
             *      _ogWidth = new image.width
             *      _ogUri = new image uri
             */
        }

        private void OnRevert()
        {
            if (_ogImage != null)
            {
                Image.Source = _ogImage;
                Image.Width = _ogWidth;
                _originalWidth = _ogWidth;

                _docCtrl.SetField<ImageController>(KeyStore.DataKey, _ogUri, true);

                //var oldpoint = _docCtrl.GetField<PointController>(KeyStore.PositionFieldKey).Data;
                //Point point = new Point(oldpoint.X - RectGeo.X, oldpoint.Y - RectGeo.Y);

                //_docCtrl.SetField<PointController>(KeyStore.PositionFieldKey, point, true);
                //_cropControl = new StateCropControl(_docCtrl, this);
            }
        }

        private void Image_Loaded(object sender, RoutedEventArgs e)
        {
            // initialize values that rely on the image
            _originalImage = Image;
            _originalWidth = Image.ActualWidth;
            _docview = this.GetFirstAncestorOfType<DocumentView>();
            _docview.OnCropClick += OnCropClick;
            _docview.OnRevert += OnRevert;
            _docview.OnReplaceImage += OnReplaceImage;
            _docview.OnRotate += OnRotate;
            _docview.OnHorizontalMirror += OnHorizontalMirror;
            _docview.OnVerticalMirror += OnVerticalMirror;
            Focus(FocusState.Keyboard);
            _cropControl = new StateCropControl(_docCtrl, this);
        }

        private void OnRotate()
        {
            Rect rect = new Rect
            {
                X = 0,
                Y = 0,
                Width = Math.Floor(Image.ActualHeight),
                Height = Math.Floor(Image.ActualWidth)
            };

            OnCrop(rect, BitmapRotation.Clockwise90Degrees);
        }

        private void OnHorizontalMirror()
        {
            MirrorImage(BitmapFlip.Horizontal);
        }

        private void OnVerticalMirror()
        {
            MirrorImage(BitmapFlip.Vertical);
        }

        private void MirrorImage(BitmapFlip flip)
        {
            Rect rect = new Rect
            {
                X = 0,
                Y = 0,
                Height = Math.Floor(Image.ActualHeight),
                Width = Math.Floor(Image.ActualWidth)
            };
            OnCrop(rect, BitmapRotation.None, flip);
        }

        // called when the cropclick action is invoked in the image subtoolbar
        private void OnCropClick()
        {
            // make sure that we aren't already cropping
            if (xGrid.Children.Contains(_cropControl)) return;
            Focus(FocusState.Programmatic);
            xGrid.Children.Add(_cropControl);
            _docview.hideControls();
            _isCropping = true;
        }

        private void StopImageFromMoving(object sender, PointerRoutedEventArgs e)
        {
            // prevent the image from being moved while being cropped
            if (_isCropping) e.Handled = true;
        }

        /// <summary>
        ///     crops the image with respect to the values of the rectangle passed in
        /// </summary>
        /// <param name="rectangleGeometry">
        ///     rectangle geometry that determines the size and starting point of the crop
        /// </param>
        private async void OnCrop(Rect rectangleGeometry, BitmapRotation rot = BitmapRotation.None, BitmapFlip flip = BitmapFlip.None)
        {
            if (_ogImage == null)
            {
                _ogImage = Image.Source;
                _ogWidth = Image.ActualWidth;
                _ogUri = _imgctrl.Data;
            }
            //_originalWidth is original width of owl, not replaced image
            var scale = _originalWidth / Image.ActualWidth;

            // retrieves data from rectangle
            var startPointX = (uint)rectangleGeometry.X;
            var startPointY = (uint)rectangleGeometry.Y;
            var height = (uint)rectangleGeometry.Height;
            var width = (uint)rectangleGeometry.Width;

            // finds local uri path of image controller's image source
            StorageFile file;

            /*
             * try catch is literally the only way we can deal with regular
             * local uris, absolute uris, and website uris as the same time
             */
            try
            {
                // method of getting file from local uri
                file = await StorageFile.GetFileFromPathAsync(_imgctrl.ImageSource.LocalPath);
            }
            catch (Exception)
            {
                // method of getting file from absolute uri
                file = await StorageFile.GetFileFromApplicationUriAsync(_imgctrl.ImageSource);
            }

            Debug.Assert(file != null); // if neither works, something's hecked up
            WriteableBitmap cropBmp;

            // opens the uri path and reads it
            using (IRandomAccessStream stream = await file.OpenReadAsync())
            {
                var decoder = await BitmapDecoder.CreateAsync(stream);

                // finds scaled size of the new bitmap image
                var scaledWidth = (uint)Math.Floor(decoder.PixelWidth / scale);
                var scaledHeight = (uint)Math.Floor(decoder.PixelHeight / scale);

                // sets the boundaries for how we are cropping the bitmap image
                var bitmapTransform = new BitmapTransform();
                var bounds = new BitmapBounds
                {
                    X = startPointX,
                    Y = startPointY,
                    Width = width,
                    Height = height
                };
                bitmapTransform.Rotation = rot;
                bitmapTransform.Flip = flip;
                bitmapTransform.Bounds = bounds;
                bitmapTransform.ScaledWidth = scaledWidth;
                bitmapTransform.ScaledHeight = scaledHeight;

                // creates a new bitmap image with those boundaries
                var pix = await decoder.GetPixelDataAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    bitmapTransform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.ColorManageToSRgb
                );

                var pixels = pix.DetachPixelData();

                // dis is it, the new bitmap image
                cropBmp = new WriteableBitmap((int)width, (int)height);
                var pixStream = cropBmp.PixelBuffer.AsStream();
                pixStream.Write(pixels, 0, (int)(width * height * 4));

                SaveCroppedImageAsync(cropBmp, decoder, rectangleGeometry, pixels);
            }
        }

        private async void SaveCroppedImageAsync(WriteableBitmap cropBmp, BitmapDecoder decoder, Rect rectgeo,
            byte[] pixels)
        {
            var width = (uint)rectgeo.Width;
            var height = (uint)rectgeo.Height;

            // randomly generate a new guid for the filename
            var fileName = UtilShared.GenerateNewId() + ".jpg"; // .jpg works for all images
            var bitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
            // create the file
            var newFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName,
                CreationCollisionOption.ReplaceExisting);

            // load the file with the iamge information
            using (var newStream = await newFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(bitmapEncoderGuid, newStream);

                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Straight,
                    width,
                    height,
                    decoder.DpiX,
                    decoder.DpiY,
                    pixels);
                await encoder.FlushAsync();
            }

            // retrieve the uri from the file to update the image controller
            var path = "ms-appdata:///local/" + newFile.Name;
            var uri = new Uri(path);
            _docCtrl.SetField<ImageController>(KeyStore.DataKey, uri, true);

            // update the image source, width, and positions
            Image.Source = cropBmp;
            Image.Width = width;

            // store new image information so that multiple crops can be made
            _originalWidth = width;
            _imgctrl = _docCtrl.GetDereferencedField(KeyStore.DataKey, _context) as ImageController;

            var oldpoint = _docCtrl.GetField<PointController>(KeyStore.PositionFieldKey).Data;
            var scale = _docCtrl.GetField<PointController>(KeyStore.ScaleAmountFieldKey).Data;
            Point point = new Point(oldpoint.X + _cropControl.GetBounds().X * scale.X, oldpoint.Y + _cropControl.GetBounds().Y * scale.Y);

            _docCtrl.SetField<PointController>(KeyStore.PositionFieldKey, point, true);
            _cropControl = new StateCropControl(_docCtrl, this);

            // TODO: Test that replace button works with cropping when merged with master
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // functionality for saving a crop and for moving the cropping boxes with directional keys
        private void XGrid_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (_isCropping)
                switch (e.Key)
                {
                    case VirtualKey.Enter:
                        // crop the image!
                        _isCropping = false;
                        xGrid.Children.Remove(_cropControl);
                        OnCrop(_cropControl.GetBounds());
                        _docview.showControls();

                        break;
                    case VirtualKey.Left:
                    case VirtualKey.Right:
                    case VirtualKey.Up:
                    case VirtualKey.Down:
                        // moves the bounding box in the key's direction
                        _cropControl.OnKeyDown(e);
                        break;
                }
            e.Handled = true;
        }

        // removes the cropping controls and allows image to be moved and used when focus is lost
        private void EditableImage_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (!_isCropping) return;
            _isCropping = false;
            _docview.showControls();
            xGrid.Children.Remove(_cropControl);
            xRegionPreview.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                var pos = e.GetCurrentPoint(xImage).Position;

                var x = Math.Min(pos.X, _anchorPoint.X);
                var y = Math.Min(pos.Y, _anchorPoint.Y);
                xRegionPreview.Margin = new Thickness(x, y, 0, 0);

                xRegionPreview.Width = Math.Abs(pos.X - _anchorPoint.X);
                xRegionPreview.Height = Math.Abs(pos.Y - _anchorPoint.Y);
            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            StopImageFromMoving(sender, e);
            if (xRegionPreview.Width < 50 && xRegionPreview.Height < 50)
            {
                xRegionPreview.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            _isDragging = false;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!_isCropping)
            {
                var pos = e.GetCurrentPoint(xImage).Position;
                _anchorPoint = pos;
                _isDragging = true;
                xRegionPreview.Margin = new Thickness(pos.X, pos.Y, 0, 0);
                xRegionPreview.Width = 0;
                xRegionPreview.Height = 0;
                xRegionPreview.Visibility = Windows.UI.Xaml.Visibility.Visible;
                StopImageFromMoving(sender, e);
            }
        }

        public bool IsSomethingSelected()
        {
            return xRegionPreview.Visibility == Windows.UI.Xaml.Visibility.Visible;
        }


        public DocumentController GetRegionDocument()
        {
            if (!this.IsSomethingSelected()) return _docCtrl;

            var imNote = new ImageNote(_imgctrl.ImageSource, new Point(xRegionPreview.Margin.Left, xRegionPreview.Margin.Top), new Size(xRegionPreview.Width, xRegionPreview.Height)).Document;

            var regions = _docCtrl.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null);
            if (regions == null)
            {
                var dregions = new List<DocumentController>();
                dregions.Add(imNote);
                _docCtrl.GetDataDocument().SetField<ListController<DocumentController>>(KeyStore.RegionsKey, dregions, true);
            }
            else
            {
                regions.Add(imNote);
            }

            var newBox = new ImageRegionBox { LinkTo = imNote };
            newBox.SetPosition(new Point(xRegionPreview.Margin.Left, xRegionPreview.Margin.Top), new Size(xRegionPreview.Width, xRegionPreview.Height), new Size(xImage.ActualWidth, xImage.ActualHeight));
            xRegionsGrid.Children.Add(newBox);
            xRegionPreview.Visibility = Windows.UI.Xaml.Visibility.Collapsed;

            return imNote;

            /*
            var dc = new RichTextNote(xRichEditBox.Document.Selection.Text).Document;
            var s1 = xRichEditBox.Document.Selection.StartPosition;
            var s2 = xRichEditBox.Document.Selection.EndPosition;
            createRTFHyperlink(dc, ref s1, ref s2, false, false);
            var regions = DataDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null);
            if (regions == null)
            {
                var dregions = new List<DocumentController>();
                dregions.Add(dc);
                DataDocument.SetField<ListController<DocumentController>>(KeyStore.RegionsKey, dregions, true);
            }
            else
            {
                regions.Add(dc);
            }
            return dc;
            */
        }

        /*
	    void createRTFHyperlink(DocumentController theDoc, ref int s1, ref int s2, bool createIfNeeded, bool forceLocal)
	    {
		    Point startPt;
		    this.xRichEditBox.Document.Selection.GetPoint(HorizontalCharacterAlignment.Center, VerticalCharacterAlignment.Baseline, PointOptions.Start, out startPt);
		    string link = "\"" + theDoc.GetId() + "\"";
		    if (!forceLocal && theDoc.GetDataDocument().DocumentType.Equals(HtmlNote.DocumentType) && (bool)theDoc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DataKey, null)?.Data?.StartsWith("http"))
		    {
			    link = "\"" + theDoc.GetDataDocument().GetDereferencedField<TextController>(KeyStore.DataKey, null).Data + "\"";
		    }

		    if (xRichEditBox.Document.Selection.Link != link)
		    {
			    if (xRichEditBox.Document.Selection.StartPosition == xRichEditBox.Document.Selection.EndPosition)
			    {
				    xRichEditBox.Document.Selection.SetText(TextSetOptions.None, theDoc.Title);
			    }

			    // set the hyperlink for the matched text
			    this.xRichEditBox.Document.Selection.Link = link;
			    // advance the end selection past the RTF embedded HYPERLINK keyword
			    s2 += this.xRichEditBox.Document.Selection.Link.Length + "HYPERLINK".Length + 1;
			    s1 = s2;
			    this.xRichEditBox.Document.Selection.CharacterFormat.BackgroundColor = Colors.LightCyan;
			    this.xRichEditBox.Document.Selection.SetPoint(startPt, PointOptions.Start, true);
		    }
	    }
		*/
        private void xRegionsGrid_OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = false;
            foreach (var box in xRegionsGrid.Children)
            {
                box.Background = new SolidColorBrush(Colors.Transparent);
            }
            (sender as UIElement).GetFirstDescendantOfType<ImageRegionBox>().Background = new SolidColorBrush(Colors.Yellow);

            //e.Handled = false;
            //var target = getHyperlinkTargetForSelection();
            //if (target != null)
            //{
            //    var theDoc = ContentController<FieldModel>.GetController<DocumentController>(target);
            //    if (DataDocument.GetDereferencedField<ListController<DocumentController>>(KeyStore.RegionsKey, null)?.TypedData.Contains(theDoc) == true)
            //    {
            //        var linkFromDoc = theDoc.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkFromKey, null);
            //        var linkToDoc = theDoc.GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkToKey, null);
            //        if (linkFromDoc != null)
            //        {
            //            var targetDoc = linkFromDoc.TypedData.First().GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkFromKey, null).TypedData.First();
            //            theDoc = targetDoc;
            //        }
            //        else if (linkToDoc != null)
            //        {

            //            var targetDoc = linkToDoc.TypedData.First().GetDataDocument().GetDereferencedField<ListController<DocumentController>>(KeyStore.LinkToKey, null).TypedData.First();
            //            theDoc = targetDoc;
            //        }
            //    }
            //    var nearest = FindNearestDisplayedTarget(e.GetPosition(MainPage.Instance), theDoc?.GetDataDocument(), this.IsCtrlPressed());
            //    if (nearest != null && !nearest.Equals(this.GetFirstAncestorOfType<DocumentView>()))
            //    {
            //        if (this.IsCtrlPressed())
            //            nearest.DeleteDocument();
            //        else MainPage.Instance.NavigateToDocumentInWorkspace(nearest.ViewModel.DocumentController, true);
            //    }
            //    else
            //    {
            //        var pt = new Point(getDocView().ViewModel.XPos + getDocView().ActualWidth, getDocView().ViewModel.YPos);
            //        if (theDoc != null)
            //        {
            //            Actions.DisplayDocument(this.GetFirstAncestorOfType<CollectionView>()?.ViewModel, theDoc.GetSameCopy(pt));
            //        }
            //        else if (target.StartsWith("http"))
            //        {
            //            if (MainPage.Instance.WebContext != null)
            //                MainPage.Instance.WebContext.SetUrl(target);
            //            else
            //            {
            //                nearest = FindNearestDisplayedBrowser(pt, target);
            //                if (nearest != null)
            //                {
            //                    if (this.IsCtrlPressed())
            //                        nearest.DeleteDocument();
            //                    else MainPage.Instance.NavigateToDocumentInWorkspace(nearest.ViewModel.DocumentController, true);
            //                }
            //                else
            //                {
            //                    theDoc = new HtmlNote(target, target, new Point(), new Size(200, 300)).Document;
            //                    Actions.DisplayDocument(this.GetFirstAncestorOfType<CollectionView>()?.ViewModel, theDoc.GetSameCopy(pt));
            //                }
            //            }
            //        }
            //    }
            //    e.Handled = true;
            //}
            //DocumentView FindNearestDisplayedBrowser(Point where, string uri, bool onlyOnPage = true)
            //{
            //    double dist = double.MaxValue;
            //    DocumentView nearest = null;
            //    foreach (var presenter in (this.GetFirstAncestorOfType<CollectionView>().CurrentView as CollectionFreeformView).xItemsControl.ItemsPanelRoot.Children.Select((c) => (c as ContentPresenter)))
            //    {
            //        var dvm = presenter.GetFirstDescendantOfType<DocumentView>();
            //        if (dvm.ViewModel.DataDocument.GetDereferencedField<TextController>(KeyStore.DataKey, null)?.Data == uri)
            //        {
            //            var mprect = dvm.GetBoundingRect(MainPage.Instance);
            //            var center = new Point((mprect.Left + mprect.Right) / 2, (mprect.Top + mprect.Bottom) / 2);
            //            if (!onlyOnPage || MainPage.Instance.GetBoundingRect().Contains(center))
            //            {
            //                var d = Math.Sqrt((where.X - center.X) * (where.X - center.X) + (where.Y - center.Y) * (where.Y - center.Y));
            //                if (d < dist)
            //                {
            //                    d = dist;
            //                    nearest = dvm;
            //                }
            //            }
            //        }
            //    }

            //    return nearest;
            //}
            //DocumentView FindNearestDisplayedTarget(Point where, DocumentController targetData, bool onlyOnPage = true)
            //{
            //    double dist = double.MaxValue;
            //    DocumentView nearest = null;
            //    foreach (var presenter in (this.GetFirstAncestorOfType<CollectionView>().CurrentView as CollectionFreeformView).xItemsControl.ItemsPanelRoot.Children.Select((c) => (c as ContentPresenter)))
            //    {
            //        var dvm = presenter.GetFirstDescendantOfType<DocumentView>();
            //        if (dvm.ViewModel.DataDocument.Id == targetData?.Id)
            //        {
            //            var mprect = dvm.GetBoundingRect(MainPage.Instance);
            //            var center = new Point((mprect.Left + mprect.Right) / 2, (mprect.Top + mprect.Bottom) / 2);
            //            if (!onlyOnPage || MainPage.Instance.GetBoundingRect().Contains(center))
            //            {
            //                var d = Math.Sqrt((where.X - center.X) * (where.X - center.X) + (where.Y - center.Y) * (where.Y - center.Y));
            //                if (d < dist)
            //                {
            //                    d = dist;
            //                    nearest = dvm;
            //                }
            //            }
            //        }
            //    }

            //    return nearest;
            //}
        }
    }
}